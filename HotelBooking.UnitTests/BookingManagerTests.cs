using System;
using System.Collections.Generic;
using HotelBooking.Core;
using HotelBooking.UnitTests.Fakes;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using Moq;


namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private IBookingManager bookingManager;
        IRepository<Booking> bookingRepository;

        public BookingManagerTests(){
            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(20);
            bookingRepository = new FakeBookingRepository(start, end);
            IRepository<Room> roomRepository = new FakeRoomRepository();
            bookingManager = new BookingManager(bookingRepository, roomRepository);
        }

        [Fact]
        public async Task FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            // Arrange
            DateTime date = DateTime.Today;

            // Act
            Task result() => bookingManager.FindAvailableRoom(date, date);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>(result);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
        {
            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);
            // Assert
            Assert.NotEqual(-1, roomId);
        }

        [Fact]
        public async Task FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom()
        {
            // This test was added to satisfy the following test design
            // principle: "Tests should have strong assertions".

            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            
            // Act
            int roomId = await bookingManager.FindAvailableRoom(date, date);

            var bookingForReturnedRoomId = (await bookingRepository.GetAllAsync()).
                Where(b => b.RoomId == roomId
                           && b.StartDate <= date
                           && b.EndDate >= date
                           && b.IsActive);
            
            // Assert
            Assert.Empty(bookingForReturnedRoomId);
        }
        
        // MOCKING FRAMEWORK TEST
        [Fact]
        public async Task CreateBooking_RoomAvailable_CreatesActiveBooking()
        {
            // Arrange

            var bookingRepository = new Mock<IRepository<Booking>>();
            var roomRepository = new Mock<IRepository<Room>>();

            // Mock the booking repository
            // No existing bookings
            bookingRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Booking>());

            // Mock the room repository
            // Pretend that there are two rooms
            roomRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Room>
                {
                    new Room { Id = 1 },
                    new Room { Id = 2 }
                });

            var bookingManager = new BookingManager(
                bookingRepository.Object,
                roomRepository.Object);

            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(5),
                CustomerId = 1
            };

            // Act
            bool result = await bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(result);
            Assert.True(booking.IsActive);
            Assert.Equal(1, booking.RoomId);

            // Verify that AddAsync was called exactly once
            bookingRepository.Verify(
                x => x.AddAsync(booking),
                Times.Once);
        }
        
        [Fact]
        public async Task CreateBooking_NoRoomsAvailable_ReturnsFalse()
        {
            // Arrange

            var bookingRepository = new Mock<IRepository<Booking>>();
            var roomRepository = new Mock<IRepository<Room>>();

            bookingRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Booking>());

            // Mock repository to return no rooms
            roomRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Room>());

            var bookingManager = new BookingManager(
                bookingRepository.Object,
                roomRepository.Object);

            var booking = new Booking
            {
                StartDate = DateTime.Today.AddDays(2),
                EndDate = DateTime.Today.AddDays(5)
            };

            // Act
            bool result = await bookingManager.CreateBooking(booking);

            // Assert
            Assert.False(result);

            // Verify that the booking was never added
            bookingRepository.Verify(
                x => x.AddAsync(It.IsAny<Booking>()),
                Times.Never);
        }
        
        [Fact]
        public async Task FindAvailableRoom_InactiveBookingDoesNotBlockRoom_ReturnsRoom()
        {
            var bookingRepository = new Mock<IRepository<Booking>>();
            var roomRepository = new Mock<IRepository<Room>>();

            DateTime startDate = DateTime.Today.AddDays(2);
            DateTime endDate = DateTime.Today.AddDays(5);

            // Room 1 has an overlapping booking,
            // but the booking is inactive.
            bookingRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Booking>
                {
                    new Booking
                    {
                        Id = 10,
                        RoomId = 1,
                        StartDate = startDate,
                        EndDate = endDate,
                        IsActive = false
                    }
                });

            roomRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Room>
                {
                    new Room { Id = 1 }
                });

            var bookingManager = new BookingManager(
                bookingRepository.Object,
                roomRepository.Object);
            
            int roomId = await bookingManager.FindAvailableRoom(
                startDate,
                endDate);
            
            Assert.Equal(1, roomId);
        }

    }
}
