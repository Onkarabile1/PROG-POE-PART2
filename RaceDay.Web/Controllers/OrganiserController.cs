using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Roles.Organiser)]
public class OrganiserController : AppController
{
    public IActionResult Dashboard() => View();
}