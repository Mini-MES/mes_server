using mes_server.Services.Interface;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace mes_server.Services
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
                var serverUrl = _configuration["OpcUa:ServerUrl"] 
                    ?? "opc.tcp://uademo.prosysopc.com:53530/OPCUA/SimulationServer";
                var appName = _configuration["OpcUa:ApplicationName"] ?? "MiniMES_OpcUaClient";

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
                            StoreType = @"Directory",
                            StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault",
                            SubjectName = appName
                        },
                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = @"Directory",
                            StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications"
                        },
                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = @"Directory",
                            StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities"
                        },
                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = @"Directory",
                            StorePath = @"%CommonApplicationData%\OPC Foundation\RejectedCertificates"
                        },
                        AutoAcceptUntrustedCertificates = true,
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

                config.CertificateValidator.CertificateValidation += (s, e) =>
                {
                    e.Accept = true;
                };

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
                    identity: new UserIdentity(new AnonymousIdentityToken()),
                    preferredLocales: null
                );

                _logger.LogInformation("✅ OPC UA 서버 세션 연결 성공!");
                await SubscribeToTagsAsync();
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
    }
}
