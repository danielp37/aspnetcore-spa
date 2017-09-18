using System;
using System.Threading.Tasks;
using website.Entities;
using aspnetcore_spa.Startup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using static AspnetCore.Identity.MongoDb.JwtModels.Constants.Strings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace aspnetcore_spa.Controllers
{
  [Route("api/[controller]")]
  public class BookCopyReservationsController : Controller
  {
    protected readonly IMongoDatabase mongoDatabase;
    protected readonly IMongoCollection<BookCopyReservation> reservationCollection;
    public BookCopyReservationsController()
    {
      mongoDatabase = MongoConfig.Database;
      reservationCollection = mongoDatabase.GetCollection<BookCopyReservation>("currentreservations");
    }

    [Authorize(Policy = "AdminUser")]
    [HttpGet]
    public async Task<IActionResult> GetCheckedOutBookCopies()
    {
      var checkedOutBooksCollection = mongoDatabase.GetCollection<CheckedOutBook>("bookscheckedout");
      var filter = Builders<CheckedOutBook>.Filter.Eq(bcr => bcr.CheckedInDate, null);

      var checkedOutBooks = await checkedOutBooksCollection.Find(filter).ToListAsync();

      return Ok(new { Data = checkedOutBooks });
    }

    [Authorize(Policy = "VolunteerUser")]
    [HttpPost]
    public async Task<IActionResult> CheckoutBookCopy([FromBody]ReservationBody body)
    {
      //TODO: Verify that the BookCopyBarCode and the StudentBarCodes both exist before creating
      if (!await BookCopyBarCodeExists(body.BookCopyBarCode))
      {
        return NotFound($"BookCopyBarCode {body.BookCopyBarCode} not found.");
      }
      if (!await StudentBarCodeExists(body.StudentBarCode))
      {
        return NotFound($"StudentBarCode {body.StudentBarCode} not found.");
      }
      await CheckInBookCopyImpl(body.BookCopyBarCode);

      var reservation = new BookCopyReservation
      {
        BookCopyBarCode = body.BookCopyBarCode,
        StudentBarCode = body.StudentBarCode,
        CheckedOutDate = DateTime.Today,
        CheckOutBy = GetVolunteerAuditForCurrentUser(),
        CreatedDate = DateTime.Now
      };
      await reservationCollection.InsertOneAsync(reservation);

      return Ok(new { Data = reservation });
    }

    [Authorize(Policy = "VolunteerUser")]
    [HttpPost("checkin/{bookBarCode}")]
    public async Task<IActionResult> CheckinBookCopy(string bookBarCode)
    {
      var isAcknowledged = await CheckInBookCopyImpl(bookBarCode);

      return isAcknowledged ? (IActionResult)Ok() : BadRequest();
    }

    private VolunteerAudit GetVolunteerAuditForCurrentUser() 
    {
      var id = User.FindFirst(JwtClaimIdentifiers.Id)?.Value;
      var firstName = User.FindFirst(ClaimTypes.GivenName)?.Value;
      var lastName = User.FindFirst(ClaimTypes.Surname)?.Value;

      return new VolunteerAudit
      {
        VolunteerId = id,
        Name = $"{firstName} {lastName}"
      };
    }

    private async Task<bool> BookCopyBarCodeExists(string bookBarCode)
    {
      var bookCollection = mongoDatabase.GetCollection<Book>("books");
      var filter = Builders<Book>.Filter.ElemMatch(b => b.BookCopies, bc => bc.BarCode == bookBarCode);
      return await bookCollection.Find(filter).AnyAsync();
    }

    private async Task<bool> StudentBarCodeExists(string studentBarCode)
    {
      var classCollection = mongoDatabase.GetCollection<Class>("classes");
      var filter = Builders<Class>.Filter.ElemMatch(c => c.Students, s => s.BarCode == studentBarCode);
      return await classCollection.Find(filter).AnyAsync();
    }

    private async Task<bool> CheckInBookCopyImpl(string bookBarCode)
    {
      var filter = Builders<BookCopyReservation>.Filter.Eq(b => b.BookCopyBarCode, bookBarCode)
                      & Builders<BookCopyReservation>.Filter.Eq(b => b.CheckedInDate, null);
      var update = Builders<BookCopyReservation>.Update
                      .Set(b => b.CheckedInDate, DateTime.Today.ToLocalTime())
                      .Set(b => b.CheckInBy, GetVolunteerAuditForCurrentUser())
                      .CurrentDate(b => b.ModifiedDate);
      var updateResult = await reservationCollection.UpdateManyAsync(filter, update);

      return updateResult.IsAcknowledged;
    }

    public class ReservationBody
    {
      public string BookCopyBarCode { get; set; }
      public string StudentBarCode { get; set; }
    }
  }
}