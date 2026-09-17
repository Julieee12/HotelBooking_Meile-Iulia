using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelBooking.Core;
using Moq;
using Xunit;

namespace HotelBooking.UnitTests
{
    public class BookingManagerMoqTests
    {
        private readonly Mock<IRepository<Booking>> bookingRepoMock;
        private readonly Mock<IRepository<Room>> roomRepoMock;
        private readonly BookingManager bookingManager;

        public BookingManagerMoqTests()
        {
            bookingRepoMock = new Mock<IRepository<Booking>>();
            roomRepoMock = new Mock<IRepository<Room>>();
            bookingManager = new BookingManager(bookingRepoMock.Object, roomRepoMock.Object);
        }

        // ---------- FindAvailableRoom: data-driven boundary tests ----------

        // Theory + InlineData lets us feed multiple date pairs through the same
        // test logic, instead of copy-pasting one test per case.
        [Theory]
        [InlineData(0)]   // today
        [InlineData(-1)]  // yesterday
        public async Task FindAvailableRoom_StartDateNotInFuture_ThrowsArgumentException(int daysFromToday)
        {
            // Arrange
            DateTime start = DateTime.Today.AddDays(daysFromToday);
            DateTime end = start.AddDays(5);

            // Act
            Task Act() => bookingManager.FindAvailableRoom(start, end);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(Act);
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateAfterEndDate_ThrowsArgumentException()
        {
            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(5);

            Task Act() => bookingManager.FindAvailableRoom(start, end);

            await Assert.ThrowsAsync<ArgumentException>(Act);
        }

        [Fact]
        public async Task FindAvailableRoom_NoBookingsExist_ReturnsFirstRoom()
        {
            // Arrange
            var rooms = new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } };
            roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

            DateTime start = DateTime.Today.AddDays(1);
            DateTime end = DateTime.Today.AddDays(2);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(start, end);

            // Assert
            Assert.Equal(1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_AllRoomsOccupied_ReturnsMinusOne()
        {
            // Arrange
            var rooms = new List<Room> { new Room { Id = 1 } };
            var bookings = new List<Booking>
            {
                new Booking
                {
                    RoomId = 1,
                    IsActive = true,
                    StartDate = DateTime.Today.AddDays(1),
                    EndDate = DateTime.Today.AddDays(10)
                }
            };
            roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings);

            // Act
            int roomId = await bookingManager.FindAvailableRoom(
                DateTime.Today.AddDays(3), DateTime.Today.AddDays(5));

            // Assert
            Assert.Equal(-1, roomId);
        }

        // ---------- CreateBooking: verifying interaction with the mock ----------

        [Fact]
        public async Task CreateBooking_RoomAvailable_ReturnsTrueAndCallsAdd()
        {
            // Arrange
            var rooms = new List<Room> { new Room { Id = 1 } };
            roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Booking>());

            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(2)
            };

            // Act
            bool result = await bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(result);
            // Verify checks that a specific method was actually called on the mock,
            // which a hand-written fake can't easily give you without extra fields.
            bookingRepoMock.Verify(r => r.AddAsync(booking), Times.Once);
        }

        [Fact]
        public async Task CreateBooking_NoRoomAvailable_ReturnsFalseAndNeverCallsAdd()
        {
            // Arrange: one room, already fully booked over the requested period
            var rooms = new List<Room> { new Room { Id = 1 } };
            var bookings = new List<Booking>
            {
                new Booking
                {
                    RoomId = 1,
                    IsActive = true,
                    StartDate = DateTime.Today.AddDays(1),
                    EndDate = DateTime.Today.AddDays(10)
                }
            };
            roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings);

            var newBooking = new Booking
            {
                StartDate = DateTime.Today.AddDays(3),
                EndDate = DateTime.Today.AddDays(4)
            };

            // Act
            bool result = await bookingManager.CreateBooking(newBooking);

            // Assert
            Assert.False(result);
            bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        // ---------- GetFullyOccupiedDates: data-driven with MemberData ----------

        // MemberData is used here instead of InlineData because the test cases
        // involve objects (lists of bookings), not just primitive values.
        public static IEnumerable<object[]> OccupiedDateCases()
        {
            DateTime today = DateTime.Today;

            // Case 1: single room, one active booking covering the whole range -> fully occupied every day
            yield return new object[]
            {
                new List<Room> { new Room { Id = 1 } },
                new List<Booking>
                {
                    new Booking { RoomId = 1, IsActive = true, StartDate = today.AddDays(1), EndDate = today.AddDays(3) }
                },
                today.AddDays(1), today.AddDays(3),
                3 // expected number of fully occupied dates
            };

            // Case 2: booking is inactive -> should not count as occupied
            yield return new object[]
            {
                new List<Room> { new Room { Id = 1 } },
                new List<Booking>
                {
                    new Booking { RoomId = 1, IsActive = false, StartDate = today.AddDays(1), EndDate = today.AddDays(3) }
                },
                today.AddDays(1), today.AddDays(3),
                0
            };

            // Case 3: two rooms, only one booked -> never fully occupied
            yield return new object[]
            {
                new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } },
                new List<Booking>
                {
                    new Booking { RoomId = 1, IsActive = true, StartDate = today.AddDays(1), EndDate = today.AddDays(3) }
                },
                today.AddDays(1), today.AddDays(3),
                0
            };
        }

               [Fact]
        public async Task GetFullyOccupiedDates_StartAfterEnd_ThrowsArgumentException()
        {
            DateTime start = DateTime.Today.AddDays(5);
            DateTime end = DateTime.Today.AddDays(1);

            Task Act() => bookingManager.GetFullyOccupiedDates(start, end);

            await Assert.ThrowsAsync<ArgumentException>(Act);
        }

        // ============================================================
        // Failure-path tests: making sure the code rejects bad stuff
        // correctly, not just accepts good stuff
        // ============================================================

        [Fact]
        public async Task CreateBooking_NullBooking_ThrowsArgumentNullException()
        {
            // nobody tests what happens when you just don't pass a booking
            Task Act() => bookingManager.CreateBooking(null);

            await Assert.ThrowsAsync<ArgumentNullException>(Act);
        }

        [Fact]
        public async Task CreateBooking_RoomUnavailable_NeverCallsAddAsync()
        {
            // room is booked solid for the whole window
            var rooms = new List<Room> { new Room { Id = 1 } };
            var bookings = new List<Booking>
            {
                new Booking { RoomId = 1, IsActive = true,
                    StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(10) }
            };
            roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            bookingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(bookings);

            var newBooking = new Booking
            {
                StartDate = DateTime.Today.AddDays(3),
                EndDate = DateTime.Today.AddDays(4)
            };

            await bookingManager.CreateBooking(newBooking);

            // returning false isn't enough, it also shouldn't sneak the booking in anyway
            bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }
    }
}