using mes_server.Models.Enum;
using mes_server.Models.Settings;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Security.Principal;
using System.Text;
using ISession = Opc.Ua.Client.ISession;

namespace mes_server.Services.OpcService
{
    public class OpcUaService : IOpcUaService
    {
        private readonly OpcUaSettings _settings;
        private readonly ILogger<OpcUaService> _logger;

        private ISession? _session;

        public event Action<OpcUaTagType>? OnDataReceived;

        public bool IsConnected => _session != null && _session.Connected;

        public OpcUaService(OpcUaSettings settings, ILogger<OpcUaService> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task ConnectAndSubscribeAsync()
        {
            try
            {
                ValidateSettings();

                _logger.LogInformation("🔌 OPC UA 서버 연결 시도: {ServerUrl}", _settings.ServerURL);

                var config = await CreateApplicationConfigurationAsync();

                var selectedEndpoint = CoreClientUtils.SelectEndpoint(config, _settings.ServerURL, useSecurity: true);

                _logger.LogInformation(
                    "선택된 OPC UA Endpoint: Url={EndpointUrl}, SecurityPolicy={SecurityPolicy}, SecurityMode={SecurityMode}",
                    selectedEndpoint.EndpointUrl,
                    selectedEndpoint.SecurityPolicyUri,
                    selectedEndpoint.SecurityMode);

                if (selectedEndpoint.SecurityPolicyUri != SecurityPolicies.Basic256Sha256)
                {
                    throw new InvalidOperationException( $"Basic256Sha256 Endpoint를 선택하지 못했습니다. 선택값: {selectedEndpoint.SecurityPolicyUri}");
                }

                if (selectedEndpoint.SecurityMode != MessageSecurityMode.SignAndEncrypt)
                {
                    throw new InvalidOperationException($"SignAndEncrypt Endpoint를 선택하지 못했습니다. 선택값: {selectedEndpoint.SecurityMode}");
                }

                var endpointConfiguration = EndpointConfiguration.Create(config);

                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                var identity = new UserIdentity(_settings.Username, Encoding.UTF8.GetBytes(_settings.Password));

                _session = await DefaultSessionFactory.Instance.CreateAsync(
                   config,
                   endpoint,
                   updateBeforeConnect: false,
                   checkDomain: false,
                   sessionName: _settings.ApplicationName,
                   sessionTimeout: (uint)_settings.SessionTimeout,
                   identity: identity,
                   preferredLocales: null);

                _logger.LogInformation("✅ OPC UA 서버 세션 연결 성공!");

                await SubscribeToTagsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ OPC UA 서버 연결 중 오류가 발생했습니다.");
                throw;
            }
        }

        private async Task<ApplicationConfiguration>
            CreateApplicationConfigurationAsync()
        {
            var pkiRoot = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MiniMES",
                "OpcUa",
                "pki");

            var ownStorePath =
                Path.Combine(pkiRoot, "own");

            var trustedStorePath =
                Path.Combine(pkiRoot, "trusted");

            var issuerStorePath =
                Path.Combine(pkiRoot, "issuers");

            var rejectedStorePath =
                Path.Combine(pkiRoot, "rejected");

            var config = new ApplicationConfiguration
            {
                ApplicationName = _settings.ApplicationName,

                ApplicationUri = Utils.Format("urn:{0}:{1}", System.Net.Dns.GetHostName(), _settings.ApplicationName),

                ApplicationType = ApplicationType.Client,

                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = ownStorePath,
                        SubjectName = $"CN={_settings.ApplicationName}"
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

                    AutoAcceptUntrustedCertificates =_settings.AutoAcceptCertificates,

                    AddAppCertToTrustedStore = true
                },

                TransportConfigurations =new TransportConfigurationCollection(),

                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 15000
                },

                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout =
                        _settings.SessionTimeout
                },

                TraceConfiguration =new TraceConfiguration()
            };

            await config.ValidateAsync(ApplicationType.Client);

            var application = new ApplicationInstance
            {
                ApplicationName = _settings.ApplicationName,

                ApplicationType = ApplicationType.Client,

                ApplicationConfiguration = config
            };

            var certificateReady = await application.CheckApplicationInstanceCertificatesAsync(false, 2048, CancellationToken.None);

            if (!certificateReady)
            {
                throw new InvalidOperationException("Mini-MES OPC UA 클라이언트 인증서를 준비하지 못했습니다.");
            }

            var clientCertificate = await config.SecurityConfiguration.ApplicationCertificate.FindAsync(true);

            if (clientCertificate == null)
            {
                throw new InvalidOperationException("생성된 OPC UA 클라이언트 인증서를 찾지 못했습니다.");
            }

            _logger.LogInformation("OPC UA 클라이언트 인증서 준비 완료: Subject={Subject}, Thumbprint={Thumbprint}",clientCertificate.Subject,clientCertificate.Thumbprint);

            if (_settings.AutoAcceptCertificates)
            {
                config.CertificateValidator.CertificateValidation +=
                    (_, eventArgs) =>
                    {
                        eventArgs.Accept = true;
                    };
            }

            return config;
        }

        private async Task SubscribeToTagsAsync()
        {
            if (_session == null || !_session.Connected) throw new InvalidOperationException("OPC UA 세션이 연결되지 않았습니다.");

            var subscription = new Subscription(_session.DefaultSubscription)
            {
                PublishingInterval = 1000,
                PublishingEnabled = true
            };

            foreach (var equipment in _settings.Equipments)
            {
                AddMonitoredItem(
                    subscription,
                    equipment.EquipmentId,
                    OpcUaTagType.Counter,
                    equipment.CounterNodeId,
                    queueSize: 100);

                AddMonitoredItem(
                    subscription,
                    equipment.EquipmentId,
                    OpcUaTagType.Running,
                    equipment.RunningNodeId,
                    queueSize: 10);

                AddMonitoredItem(
                    subscription,
                    equipment.EquipmentId,
                    OpcUaTagType.Temperature,
                    equipment.TemperatureNodeId,
                    queueSize: 10);
            }

            _session.AddSubscription(subscription);
            await subscription.CreateAsync();
            await subscription.SetPublishingModeAsync(true);

            _logger.LogInformation("OPC UA 태그 구독 완료: EquipmentCount={EquipmentCount}, TagCount={TagCount}", _settings.Equipments.Count, subscription.MonitoredItemCount);
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

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.ServerURL))
            {
                throw new InvalidOperationException("OpcUa:ServerUrl 설정이 없습니다.");
            }

            if (string.IsNullOrWhiteSpace(_settings.ApplicationName))
            {
                throw new InvalidOperationException("OpcUa:ApplicationName 설정이 없습니다.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Username) || string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException("Kepware OPC UA 접속 계정이 설정되지 않았습니다.");
            }

            if (_settings.Equipments.Count == 0)
            {
                throw new InvalidOperationException("OPC UA 설비 설정이 없습니다.");
            }
        }
        private void AddMonitoredItem(Subscription subscription, string equipmentId, OpcUaTagType tagType, string nodeIdValue, uint queueSize)
        {
            if (string.IsNullOrWhiteSpace(nodeIdValue))
            {
                throw new InvalidOperationException(
                    $"{equipmentId}.{tagType} NodeId가 설정되지 않았습니다.");
            }

            var context = new OpcUaMonitoredItemContext(
                equipmentId,
                tagType,
                nodeIdValue);

            var item =
                new MonitoredItem(
                    subscription.DefaultItem)
                {
                    DisplayName =
                        $"{equipmentId}.{tagType}",

                    StartNodeId =
                        NodeId.Parse(nodeIdValue),

                    SamplingInterval = 1000,

                    QueueSize = queueSize,

                    DiscardOldest = true,

                    Handle = context
                };

            item.Notification += HandleNotification;

            subscription.AddItem(item);
        }

        private void HandleNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs eventArgs)
        {
            if (eventArgs.NotificationValue is not MonitoredItemNotification notification)
            {
                return;
            }

            var dataValue = notification.Value;

            if (dataValue?.Value == null)
            {
                return;
            }

            if (monitoredItem.Handle is not OpcUaMonitoredItemContext context)
            {
                _logger.LogWarning("MonitoredItem Context가 없습니다: {DisplayName}", monitoredItem.DisplayName);

                return;
            }

            var timestamp = dataValue.SourceTimestamp == DateTime.MinValue ? DateTime.UtcNow : dataValue.SourceTimestamp;

            _logger.LogDebug(
                "OPC UA 데이터 수신: Equipment={EquipmentId}, Tag={TagType}, Value={Value}",
                context.EquipmentId,
                context.TagType,
                dataValue.Value);

            OnDataReceived?.Invoke(new OpcUaTagEvent(
                    context.EquipmentId,
                    context.TagType,
                    context.NodeId,
                    dataValue.Value,
                    timestamp)
                );
        }
    }
}
