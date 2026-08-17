using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace mes_server.Services.OpcService
{
    public class OpcUaService : IOpcUaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpcUaService> _logger;
        private Opc.Ua.Client.ISession? _session;

        public event Action<string, object, DateTime>? OnDataReceived;

        public bool IsConnected => _session != null && _session.Connected;

        public OpcUaService(IConfiguration configuration, ILogger<OpcUaService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ConnectAndSubscribeAsync()
        {
            try
            {
                var serverUrl = _configuration["OPC_UA:ServerURL"] ?? throw new InvalidOperationException("OPC UA 서버 URL이 구성에 없습니다.");
                var appName = _configuration["OPC_UA:ApplicationName"] ?? throw new InvalidOperationException("OPC UA 애플리케이션 이름이 구성에 없습니다.");

                var username = _configuration["OPC_UA:Username"];
                var password = _configuration["OPC_UA:Password"];

                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException(
                        "Kepware OPC UA 접속 계정이 설정되지 않았습니다.");
                }

                var pkiRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MiniMES",
                    "OpcUa",
                    "pki");

                var ownStorePath = Path.Combine(pkiRoot, "own");
                var trustedStorePath = Path.Combine(pkiRoot, "trusted");
                var issuerStorePath = Path.Combine(pkiRoot, "issuers");
                var rejectedStorePath = Path.Combine(pkiRoot, "rejected");

                _logger.LogInformation("🔌 OPC UA 서버 연결 시도: {ServerUrl}", serverUrl);

                var config = new ApplicationConfiguration()
                {
                    ApplicationName = appName,
                    ApplicationUri = Utils.Format(@"urn:{0}:{1}", System.Net.Dns.GetHostName(), appName),
                    ApplicationType = ApplicationType.Client,
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = "Directory",
                            StorePath = ownStorePath,
                            SubjectName = $"CN={appName}"
                        },
                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = trustedStorePath
                        },
                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = issuerStorePath
                        },
                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = rejectedStorePath
                        },

                        AutoAcceptUntrustedCertificates = false,
                        AddAppCertToTrustedStore = true
                    },
                    TransportConfigurations = new TransportConfigurationCollection(),
                    TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                    ClientConfiguration = new ClientConfiguration
                    {
                        DefaultSessionTimeout = 60000
                    },
                    TraceConfiguration = new TraceConfiguration()
                };

                await config.ValidateAsync(ApplicationType.Client);

                var application = new ApplicationInstance
                {
                    ApplicationName = appName,
                    ApplicationType = ApplicationType.Client,
                    ApplicationConfiguration = config
                };

                var certificateReady =
                    await application.CheckApplicationInstanceCertificatesAsync(
                        false,
                        2048,
                        CancellationToken.None);

                if (!certificateReady)
                {
                    throw new InvalidOperationException(
                        "Mini-MES OPC UA 클라이언트 인증서를 생성하거나 불러오지 못했습니다.");
                }

                var clientCertificate =
                    await config.SecurityConfiguration.ApplicationCertificate
                        .FindAsync(true);

                if (clientCertificate == null)
                {
                    throw new InvalidOperationException(
                        "생성된 Mini-MES OPC UA 클라이언트 인증서를 찾지 못했습니다.");
                }

                _logger.LogInformation(
                    "OPC UA 클라이언트 인증서 준비 완료: Subject={Subject}, Thumbprint={Thumbprint}, Store={StorePath}",
                    clientCertificate.Subject,
                    clientCertificate.Thumbprint,
                    ownStorePath);

                var selectedEndpoint = CoreClientUtils.SelectEndpoint(config, serverUrl, useSecurity: false);
                var endpointConfiguration = EndpointConfiguration.Create(config);
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                _session = await DefaultSessionFactory.Instance.CreateAsync(
                    config,
                    endpoint,
                    updateBeforeConnect: false,
                    checkDomain: false,
                    sessionName: appName,
                    sessionTimeout: 60000u,
                    identity: new UserIdentity(username, System.Text.Encoding.UTF8.GetBytes(password)),
                    preferredLocales: null
                );

                _logger.LogInformation("✅ OPC UA 서버 세션 연결 성공!");
                BrowseAllNodes();
                BrowseRootObjects();
                // await SubscribeToTagsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ OPC UA 서버 연결 중 오류가 발생했습니다.");
                throw;
            }
        }

        private async Task SubscribeToTagsAsync()
        {
            if (_session == null || !_session.Connected) return;

            // MES 핵심 3가지 센서 태그만 지정 (Counter: 실적, Sinusoid: 온도, Square: 상태)
            var targetTags = new (string DisplayName, NodeId NodeId)[]
            {
                ("Counter",  new NodeId(1001, 3)), // ns=3;i=1001 (생산 누적 수량)
                ("Sinusoid", new NodeId(1004, 3)), // ns=3;i=1004 (설비 온도)
                ("Square",   new NodeId(1005, 3))  // ns=3;i=1005 (가동/대기 상태)
            };

            var subscription = new Subscription(_session.DefaultSubscription)
            {
                PublishingInterval = 1000,
                PublishingEnabled = true
            };

            foreach (var (displayName, nodeId) in targetTags)
            {
                var item = new MonitoredItem(subscription.DefaultItem)
                {
                    DisplayName = displayName,
                    StartNodeId = nodeId,
                    SamplingInterval = 1000,
                    QueueSize = 10,
                    DiscardOldest = true
                };

                item.Notification += (monitoredItem, e) =>
                {
                    if (e.NotificationValue is MonitoredItemNotification notification && notification.Value != null)
                    {
                        var dataValue = notification.Value;
                        if (dataValue.Value != null)
                        {
                            OnDataReceived?.Invoke(monitoredItem.DisplayName, dataValue.Value, dataValue.SourceTimestamp);
                        }
                    }
                };

                subscription.AddItem(item);
            }

            _session.AddSubscription(subscription);
            await subscription.CreateAsync();
            await subscription.SetPublishingModeAsync(true);
        }

        public async Task DisconnectAsync()
        {
            if (_session != null)
            {
                await _session.CloseAsync();
                _session.Dispose();
                _session = null;
            }
        }

        private void BrowseRootObjects()
        {
            if (_session == null || !_session.Connected)
            {
                _logger.LogWarning("OPC UA 세션이 연결되지 않았습니다.");
                return;
            }

            var browser = new Browser(_session)
            {
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                NodeClassMask =
                    (int)NodeClass.Object |
                    (int)NodeClass.Variable
            };

            var references = browser.Browse(ObjectIds.ObjectsFolder);

            foreach (var reference in references)
            {
                var nodeId = ExpandedNodeId.ToNodeId(
                    reference.NodeId,
                    _session.NamespaceUris);

                if (nodeId == null)
                    continue;

                _logger.LogInformation(
                    "ROOT: {DisplayName} | Class={NodeClass} | NodeId={NodeId} | Namespace={Namespace}",
                    reference.DisplayName.Text,
                    reference.NodeClass,
                    nodeId,
                    nodeId.NamespaceIndex);
            }
        }

        private void BrowseAllNodes()
        {
            if (_session == null || !_session.Connected)
            {
                _logger.LogWarning("OPC UA 세션이 연결되지 않았습니다.");
                return;
            }

            var visited = new HashSet<string>();

            var cncChannelNodeId = NodeId.Parse("ns=2;s=CNC");

            BrowseRecursive(
                cncChannelNodeId,
                depth: 0,
                maxDepth: 5);

            void BrowseRecursive(
                NodeId parentNodeId,
                int depth,
                int maxDepth)
            {
                if (_session == null || depth > maxDepth)
                    return;

                if (!visited.Add(parentNodeId.ToString()))
                    return;

                var browser = new Browser(_session)
                {
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask =
                        (int)NodeClass.Object |
                        (int)NodeClass.Variable
                };

                ReferenceDescriptionCollection references;

                try
                {
                    references = browser.Browse(parentNodeId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "노드 탐색 실패: {NodeId}",
                        parentNodeId);

                    return;
                }

                foreach (var reference in references)
                {
                    var childNodeId = ExpandedNodeId.ToNodeId(
                        reference.NodeId,
                        _session.NamespaceUris);

                    if (childNodeId == null)
                        continue;

                    // OPC UA 표준 Server 진단 노드는 제외
                    if (childNodeId == ObjectIds.Server)
                        continue;

                    // Kepware 태그는 일반적으로 사용자 namespace에 위치하므로
                    // 표준 namespace 0의 진단/설정 노드는 출력하지 않음
                    if (childNodeId.NamespaceIndex == 0)
                        continue;

                    var indent = new string(' ', depth * 2);

                    if (reference.NodeClass == NodeClass.Variable)
                    {
                        try
                        {
                            var dataValue = _session.ReadValue(childNodeId);

                            _logger.LogInformation(
                                "{Indent}- {DisplayName} | Class={NodeClass} | NodeId={NodeId} | Value={Value} | Type={Type} | Status={Status}",
                                indent,
                                reference.DisplayName.Text,
                                reference.NodeClass,
                                childNodeId,
                                dataValue.Value,
                                dataValue.Value?.GetType().Name ?? "null",
                                dataValue.StatusCode);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "태그 값 읽기 실패: {NodeId}",
                                childNodeId);
                        }
                    }
                    else
                    {
                        _logger.LogInformation(
                            "{Indent}- {DisplayName} | Class={NodeClass} | NodeId={NodeId}",
                            indent,
                            reference.DisplayName.Text,
                            reference.NodeClass,
                            childNodeId);
                    }

                    if (reference.NodeClass == NodeClass.Object)
                    {
                        var cncChannelNodeId = NodeId.Parse("ns=2;s=CNC");
                        BrowseRecursive(
                            childNodeId,
                            depth + 1,
                            maxDepth);
                    }
                }
            }
        }
    }
}
