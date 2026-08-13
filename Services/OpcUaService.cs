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
                    ?? _configuration["OpcUa:ServerURL"] 
                    ?? "opc.tcp://uademo.prosysopc.com:53530/OPCUA/SimulationServer";
                var appName = _configuration["OpcUa:ApplicationName"] ?? "MiniMES_OpcUaClient";

                _logger.LogInformation("OPC UA 데모 서버 연결 시도: {ServerUrl}", serverUrl);

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
                    e.Accept = true; // 데모 서버 자동 수락
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

                _logger.LogInformation("OPC UA 서버 세션 연결 성공!");
                await SubscribeToTagsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OPC UA 서버 연결 및 구독 설정 중 오류가 발생했습니다.");
                throw;
            }
        }

        private async Task SubscribeToTagsAsync()
        {
            if (_session == null || !_session.Connected) return;

            var subscription = new Subscription(_session.DefaultSubscription)
            {
                PublishingInterval = 1000 // 1초 주기로 데이터 수집
            };

            // Prosys Demo Server의 Namespace Index = 2 (ns=2;s=태그명)
            var targetTags = new[] { "Sawtooth", "Sinusoid", "Random", "Counter" };

            foreach (var tagName in targetTags)
            {
                var item = new MonitoredItem(subscription.DefaultItem)
                {
                    DisplayName = tagName,
                    StartNodeId = new NodeId(tagName, 2)
                };

                item.Notification += (monitoredItem, e) =>
                {
                    foreach (var value in monitoredItem.DequeueValues())
                    {
                        if (value.Value != null)
                        {
                            _logger.LogInformation("OPC UA 데이터 수신 -> [{TagName}]: {Value}", monitoredItem.DisplayName, value.Value);
                            OnDataReceived?.Invoke(monitoredItem.DisplayName, value.Value, value.SourceTimestamp);
                        }
                    }
                };

                subscription.AddItem(item);
            }

            _session.AddSubscription(subscription);
            await subscription.CreateAsync();
            _logger.LogInformation("OPC UA 태그 구독 등록 완료! (Sawtooth, Sinusoid, Random, Counter)");
        }

        public async Task DisconnectAsync()
        {
            if (_session != null)
            {
                _logger.LogInformation("OPC UA 서버 연결 해제 중...");
                await _session.CloseAsync();
                _session.Dispose();
                _session = null;
            }
        }
    }
}
