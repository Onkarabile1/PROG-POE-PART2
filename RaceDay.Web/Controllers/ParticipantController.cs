using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Roles.Participant)]
public class ParticipantController : AppController
{
    public IActionResult MyEnrolments() => View();
}