namespace UserService.API.StartupExtensions
{
    public class ConsulConfig
    {
        public string ServiceName { get; set; }
        public string ServiceId { get; set; }
        public string ServiceAddress { get; set; }
        public int ServicePort { get; set; }
        public string ConsulAddress { get; set; }
    }
}
