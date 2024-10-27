using RapidStreamer.Application.Channels.ChannelProgramsDescriptors;
using RapidStreamer.Application.Channels.ChannelProgramsDescriptors.DataTypes;
using RapidStreamer.Application.Channels.Metadata;
using RapidStreamer.BuildingBlocks.Application.Enums;

namespace RapidStreamer.Channels.ResourceMonitoring
{
    public
#if !DEBUG
        sealed
#endif
        class ResourceMonitoringChannelMetadata : AbstractChannelMetadata<ResourceMonitoringChannel>
    {
        public override ChannelProgramsDescriptorCollection ChannelProgramsDescriptors
            => new()
            {
                new SubscribingKeyChannelProgramsDescriptor(0,
                    nameof(ResourceMonitoringChannelFeederMessage.Key),
                    "the key, key must be set \"ResourceMonitoring\"").SetTable(nameof(ResourceMonitoring)),
                new JsonChannelProgramsDescriptor(1,
                    nameof(ResourceMonitoringChannelFeederMessage.Alert),
                    "the alert").SetTable(nameof(ResourceMonitoring)),
                new SubscribingKeyChannelProgramsDescriptor(2,
                    nameof(ResourceMonitoringChannelFeederMessage.DateTime),
                    "the date and time").SetTable($"{nameof(ResourceMonitoring)}Row"),
                new PercentChannelProgramsDescriptor(3,
                    nameof(ResourceMonitoringChannelFeederMessage.CpuUsedPercentage),
                    "Gets the CPU utilization percentage.").SetTable($"{nameof(ResourceMonitoring)}Row"),
                new PercentChannelProgramsDescriptor(4,
                    nameof(ResourceMonitoringChannelFeederMessage.MemoryUsedPercentage),
                    "Gets the memory utilization percentage.").SetTable($"{nameof(ResourceMonitoring)}Row"),
                new NumberChannelProgramsDescriptor(5,
                    nameof(ResourceMonitoringChannelFeederMessage.MemoryUsedInBytes),
                    "Gets the total memory used.").SetTable($"{nameof(ResourceMonitoring)}Row"),
                new NumberChannelProgramsDescriptor(6,
                    nameof(ResourceMonitoringChannelFeederMessage.GuaranteedMemoryInBytes),
                    "Gets the memory allocated to the system in bytes.").SetTable($"{nameof(ResourceMonitoring)}Row"),
                new NumberChannelProgramsDescriptor(7,
                    nameof(ResourceMonitoringChannelFeederMessage.MaximumMemoryInBytes),
                    "Gets the container's request memory limit or the maximum allocated for the VM.").SetTable($"{nameof(ResourceMonitoring)}Row"),
                new NumberChannelProgramsDescriptor(8,
                    nameof(ResourceMonitoringChannelFeederMessage.GuaranteedCpuUnits),
                    """
                    This value corresponds to the number of the guaranteed CPUs as described by Kubernetes CPU request parameter. Each 1000 CPU units
                    represent 1 CPU or 1 Core. For example, if the Pod is configured with 1500m units as the CPU request, this property will be assigned
                    to 1.5, which means one and a half CPU will be dedicated for the Pod.
                    For a Pod, this value is calculated based on the <c>cgroupv2</c> weight, using the formula
                    <c>y = (1 + ((x - 2) * 9999) / 262142)</c>, where <c>y</c> is the CPU weight and <c>x</c> is the CPU share (<c>cgroup v1</c>).
                    For more information, see <see href="https://github.com/kubernetes/enhancements/tree/master/keps/sig-node/2254-cgroup-v2#phase-1-convert-from-cgroups-v1-settings-to-v2" />.
                    """).SetTable($"{nameof(ResourceMonitoring)}Row"),
                new NumberChannelProgramsDescriptor(9,
                    nameof(ResourceMonitoringChannelFeederMessage.MaximumCpuUnits),
                    """
                    This value corresponds to the number of the maximum CPUs as described by Kubernetes CPU limit parameter. Each 1000 CPU units
                    represent 1 CPU or 1 Core. For example, if the Pod is configured with 1500m units as the CPU limit, this property will be assigned
                    to 1.5, which means one and a half CPU will be the maximum CPU available.
                    """).SetTable($"{nameof(ResourceMonitoring)}Row"),
                new NumberChannelProgramsDescriptor(10,
                    nameof(ResourceMonitoringChannelFeederMessage.Processes),
                    "Gets count of system processes running on the local computer.").SetTable($"{nameof(ResourceMonitoring)}Row"),
                new NumberChannelProgramsDescriptor(11,
                    nameof(ResourceMonitoringChannelFeederMessage.Threads),
                    "Gets count of threads that are running in the associated processes.").SetTable($"{nameof(ResourceMonitoring)}Row"),
            };

        public ResourceMonitoringChannelMetadata()
        {
            SetChannelSnapshot(true, TimeSpan.FromMinutes(5), isTimeSeries: true);
        }
    }
}