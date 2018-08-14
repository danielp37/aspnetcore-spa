using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeReadingLibrary.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.ComponentModel.DataAnnotations;

namespace HomeReadingLibrary.Controllers.Controllers
{

  public class ClassesController : EntityController<Class>
  {
    private const int BarCodeLength = 9;
    private static readonly Random rand = new Random();
    private readonly IMongoCollection<Class> classCollection;
    public ClassesController(IMongoDatabase mongoDatabase) : base("classes", mongoDatabase)
    {
      classCollection = this.mongoDatabase.GetCollection<Class>(_collectionName);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
      var filter = Builders<Class>.Filter.Empty;

      var entityCount = await classCollection.Find(filter).CountDocumentsAsync();
      var entities = await classCollection.Find(filter)
          .SortBy(b => b.Grade)
          .ThenBy(b => b.TeacherName)
          .ToListAsync();

      return Ok(new
      {
        Count = entityCount,
        Data = entities
      });
    }

    [Authorize(Policy = "AdminUser")]
    [HttpPost("{classId}/students")]
    public async Task<IActionResult> AddStudentToClass(string classId, [FromBody]StudentBody body)
    {
      return await AddStudentToClass(classId, body.FirstName, body.LastName, await GenerateUniqueBarCode());
    }


    [Authorize(Policy = "VolunteerUser")]
    [HttpPost("{classId}/newstudent")]
    public async Task<IActionResult> AddNewStudent(string classId, [FromBody]NewStudentBody newStudent)
    {
      if (ModelState.IsValid)
      {
        if (!await DoesBarCodeAlreadyExist(newStudent.BarCode))
        {
          return await AddStudentToClass(classId, newStudent.FirstName, newStudent.LastName, newStudent.BarCode);
        }
        else
        {
          ModelState.AddModelError("BarCode", $"BarCode {newStudent.BarCode} already exists. Maybe they've already been added.");
        }
      }

      return this.BadRequest(ModelState);
    }

    private async Task<IActionResult> AddStudentToClass(string classId, string firstName, string lastName, string barCode)
    {
      var filter = Builders<Class>.Filter.Eq(c => c.ClassId, classId);
      var update = Builders<Class>.Update.AddToSet(c => c.Students, new Student
      {
        BarCode = barCode,
        FirstName = firstName,
        LastName = lastName,
        CreatedDate = DateTime.Now
      })
          .CurrentDate(c => c.ModifiedDate);
      await classCollection.FindOneAndUpdateAsync(filter, update);
      // The class returned by the above method is the version before the update
      // so we must requery to get the new version.
      var @class = classCollection.AsQueryable()
                      .Where(c => c.ClassId == classId)
                      .SingleOrDefault();

      if (@class == null)
      {
        return NotFound();
      }

      return Ok(new { Data = @class });
    }


    [Authorize(Policy = "VolunteerUser")]
    [HttpGet("/api/students/{studentBarCode}")]
    public async Task<IActionResult> GetStudentByBarCode(string studentBarCode)
    {
      var filter = Builders<Class>.Filter.ElemMatch(cls => cls.Students, student => student.BarCode == studentBarCode);
      var studentInfo = await classCollection.Find(filter)
          .Project(c => new
          {
            c.TeacherName,
            Student = c.Students.SingleOrDefault(s => s.BarCode == studentBarCode)
          })
          .FirstOrDefaultAsync();

      if (studentInfo == null)
      {
        return NotFound($"Student with barcode {studentBarCode} not found");
      }

      return Ok(studentInfo);
    }

    private async Task<string> GenerateUniqueBarCode()
    {
      var isMatch = false;
      var barCode = string.Empty;
      var attempts = 0;
      do
      {
        barCode = GetBarCode();
        isMatch = await DoesBarCodeAlreadyExist(barCode);
        attempts++;
      } while (isMatch || attempts < 10);

      if (attempts >= 10 && isMatch)
      {
        return "CouldNotFindBarCode";
      }
      return barCode;
    }

    private async Task<bool> DoesBarCodeAlreadyExist(string barCode)
    {
      var filter = Builders<Class>.Filter.ElemMatch(cls => cls.Students, student => student.BarCode == barCode);
      return await classCollection.Find(filter).AnyAsync();
    }

    private string GetBarCode()
    {
      lock (rand)
      {
        return DateTime.Today.Year.ToString() + string.Concat(Enumerable.Range(1, BarCodeLength).Select(i => rand.Next(9).ToString()));
      }
    }

    public class StudentBody
    {
      public string FirstName { get; set; }
      public string LastName { get; set; }
    }

    public class NewStudentBody
    {
      [Required]
      public string FirstName { get; set; }
      [Required]
      public string LastName { get; set; }
      [Required]
      [RegularExpression(@"2017\d{9}")]
      public string BarCode { get; set; }
    }
  }
}