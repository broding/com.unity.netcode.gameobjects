#if MULTIPLAYER_TOOLS
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Netcode.RuntimeTests.Metrics.Utility;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests.Metrics
{
    internal class ServerLogsMetricTests : SingleClientMetricTestBase
    {
        // Header is dynamically sized due to packing, will be 2 bytes for all test messages.
        private const int k_MessageHeaderSize = 2;
        private static readonly int k_ServerLogSentMessageOverhead = 2 + k_MessageHeaderSize;
        private static readonly int k_ServerLogReceivedMessageOverhead = 2;

        [UnityTest]
        [Ignore("Snapshot transition")]
        public IEnumerator TrackServerLogSentMetric()
        {
            var waitForSentMetric = new WaitForMetricValues<ServerLogEvent>(ClientMetrics.Dispatcher, NetworkMetricTypes.ServerLogSent);

            var message = Guid.NewGuid().ToString();
            NetworkLog.LogWarningServer(message);

            yield return waitForSentMetric.WaitForMetricsReceived();

            var sentMetrics = waitForSentMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, sentMetrics.Count);

            var sentMetric = sentMetrics.First();
            Assert.AreEqual(Server.LocalClientId, sentMetric.Connection.Id);
            Assert.AreEqual((uint)NetworkLog.LogType.Warning, (uint)sentMetric.LogLevel);
            Assert.AreEqual(message.Length + k_ServerLogSentMessageOverhead, sentMetric.BytesCount);
        }

        [UnityTest]
        [Ignore("Snapshot transition")]
        public IEnumerator TrackServerLogReceivedMetric()
        {
            var waitForReceivedMetric = new WaitForMetricValues<ServerLogEvent>(ServerMetrics.Dispatcher, NetworkMetricTypes.ServerLogReceived);

            var message = Guid.NewGuid().ToString();
            NetworkLog.LogWarningServer(message);

            yield return waitForReceivedMetric.WaitForMetricsReceived();

            var receivedMetrics = waitForReceivedMetric.AssertMetricValuesHaveBeenFound();
            Assert.AreEqual(1, receivedMetrics.Count);

            var receivedMetric = receivedMetrics.First();
            Assert.AreEqual(Client.LocalClientId, receivedMetric.Connection.Id);
            Assert.AreEqual((uint)NetworkLog.LogType.Warning, (uint)receivedMetric.LogLevel);
            Assert.AreEqual(message.Length + k_ServerLogReceivedMessageOverhead, receivedMetric.BytesCount);
        }
    }
}
#endif
