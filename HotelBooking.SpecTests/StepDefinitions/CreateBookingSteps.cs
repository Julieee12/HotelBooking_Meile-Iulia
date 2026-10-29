namespace HotelBooking.SpecTests.StepDefinitions;
using HotelBooking.Core;
using Moq;
using Reqnroll;
using Xunit;


[Binding]
public class CreateBookingSteps
{
    // Reqnroll creates a new instance of this class for every scenario,
    // so the fields below start empty in each scenario.
    private readonly List<Room> rooms = new();
    private readonly List<Booking> bookings = new();
    private readonly BookingManager bookingManager;

    private Booking? newBooking;
    private bool? created;
    private Exception? exception;

    public CreateBookingSteps()
    {
        // The repositories are mocks that return the lists above,
        // so the Given steps only have to fill the lists.
        var roomRepository = new Mock<IRepository<Room>>();
        roomRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => (IEnumerable<Room>)rooms);

        var bookingRepository = new Mock<IRepository<Booking>>();
        bookingRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => (IEnumerable<Booking>)bookings);

        bookingManager = new BookingManager(bookingRepository.Object, roomRepository.Object);
    }

    [Given(@"the hotel has (\d+) rooms?")]
    public void GivenTheHotelHasRooms(int numberOfRooms)
    {
        for (int id = 1; id <= numberOfRooms; id++)
        {
            rooms.Add(new Room { Id = id });
        }
    }

    [Given(@"room (\d+) has an? (active|inactive) booking from day (-?\d+) to day (-?\d+)")]
    public void GivenARoomHasABooking(int roomId, string state, int startDay, int endDay)
    {
        bookings.Add(new Booking
        {
            Id = bookings.Count + 1,
            RoomId = roomId,
            IsActive = state == "active",
            StartDate = DateTime.Today.AddDays(startDay),
            EndDate = DateTime.Today.AddDays(endDay)
        });
    }

    [When(@"I create a booking from day (-?\d+) to day (-?\d+)")]
    public async Task WhenICreateABooking(int startDay, int endDay)
    {
        newBooking = new Booking
        {
            StartDate = DateTime.Today.AddDays(startDay),
            EndDate = DateTime.Today.AddDays(endDay)
        };

        try
        {
            created = await bookingManager.CreateBooking(newBooking);
        }
        catch (ArgumentException ex)
        {
            exception = ex;
        }
    }

    [Then("the outcome should be {string}")]
    public void ThenTheOutcomeShouldBe(string expected)
    {
        // created = CreateBooking returned true
        // refused = CreateBooking returned false (no free room)
        // invalid = CreateBooking threw an ArgumentException (bad dates)
        string actual = exception != null ? "invalid" : created == true ? "created" : "refused";

        Assert.Equal(expected, actual);
    }

    [Then(@"the booking is assigned to room (\d+)")]
    public void ThenTheBookingIsAssignedToRoom(int expectedRoomId)
    {
        Assert.NotNull(newBooking);
        Assert.Equal(expectedRoomId, newBooking!.RoomId);
    }
}