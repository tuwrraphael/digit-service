using Digit.DeviceSynchronization.Impl;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Service;
using DigitPushService.Client;
using DigitService.Impl.EF;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace DigitService.Test
{
    public class DeviceSynchronizationTests
    {
        const string userId = "12345";

        public class RequestSynchronizationAsync
        {
            [Fact]
            public async void StoredTwice_UpdatesChannel()
            {
                var deviceSyncStore = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                deviceSyncStore.Setup(v => v.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DeviceSyncRequest>()))
                    .Returns(Task.CompletedTask);
                var pushClient = new Mock<IDigitPushServiceClient>()
                    .Setup(v => v.PushChannels[It.IsAny<string>()].)
            }

            [Fact]
            public async void StoredForOtherUser_Exeception()
            {

            }


        }

        public class TriggerSynchronizationAsync
        {
            [Fact]
            public async void DigestChanged_PushExecuted()
            {
                const string userId = "user";
                const string itemId = "item";
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict)
                    .Setup(v => v.GetForUserAsync(userId))
                    .Returns(Task.FromResult(new[]  { new Device() {
                        Id = "device1",
                        FocusItemDigest = "digest1",
                        FocusItemId = itemId
                    },new Device() {
                        Id = "device2",
                        FocusItemDigest = "digest1",
                        FocusItemId = itemId
                    } }));
                
                var service = new DeviceSynchronization(focusStoreMock.Object,
                    digitPushServiceClientMock.Object,
                    deviceSyncStoreMock.Object);
                await service.TriggerSynchronizationAsync(userId);

            }

            [Fact]
            public async void NoChanges_NoPush()
            {

            }
        }
    }
}
