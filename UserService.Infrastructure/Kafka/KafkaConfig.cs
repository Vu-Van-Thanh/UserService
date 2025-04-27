using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Infrastructure.Kafka
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public Dictionary<string, List<string>> ConsumeTopicNames { get; set; }
        public Dictionary<string, string> ProducerTopicNames { get; set; }
    }
}
