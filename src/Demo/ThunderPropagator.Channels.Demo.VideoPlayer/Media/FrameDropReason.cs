namespace ThunderPropagator.Channels.Demo.VideoPlayer.Media
{
    /// <summary>
    /// Why a frame (or an item queued for one subscriber) was dropped — #219's own scope, "Record
    /// decoder-queue and subscriber-drop reasons." Observed via the optional callback
    /// <see cref="DecodedFrameBuffer"/>/<see cref="SubscriberFrameQueue{T}"/> accept; recording/
    /// aggregating it into real metrics is #235's own scope.
    /// </summary>
    public enum FrameDropReason
    {
        /// <summary>A <see cref="DecodedFrameBuffer"/> was already at capacity when a new frame arrived — decode is outpacing publication.</summary>
        DecodeBufferCapacityExceeded,

        /// <summary>A buffered frame was discarded because a newer one already covers the requested media position — normal "catch up to live" behavior, not an overload signal.</summary>
        SupersededByNewerFrame,

        /// <summary>A <see cref="SubscriberFrameQueue{T}"/> was already at capacity when a new item arrived for that subscriber — that specific subscriber is too slow to keep up.</summary>
        SubscriberQueueCapacityExceeded
    }
}
