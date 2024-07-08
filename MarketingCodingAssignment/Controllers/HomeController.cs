using MarketingCodingAssignment.Models;
using MarketingCodingAssignment.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Web;

namespace MarketingCodingAssignment.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SearchEngine _searchEngine;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _searchEngine = new SearchEngine();
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public JsonResult Autocomplete(string term)
        {
            var suggestions = _searchEngine.GetSuggestions(term);
            return Json(suggestions);
        }

        [HttpGet]
        public JsonResult Search(string searchString, int start, int rows, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum, DateTime? releaseDateStart, DateTime? releaseDateEnd)
        {
            var decodedSearchString = HttpUtility.UrlDecode(searchString);
            var correctedTerm = _searchEngine.GetCorrectedTerm(decodedSearchString);
            SearchResultsViewModel searchResults = _searchEngine.Search(correctedTerm, start, rows, durationMinimum, durationMaximum, voteAverageMinimum, releaseDateStart, releaseDateEnd);
            return Json(new { searchResults });
        }

        public ActionResult ReloadIndex()
        {
            DeleteIndex();
            PopulateIndex();
            return RedirectToAction("Index", "Home");
        }

        public void DeleteIndex()
        {
            _searchEngine.DeleteIndex();
            return;
        }

        public void PopulateIndex()
        {
            _searchEngine.PopulateIndexFromCsv();
            return;
        }
    }
}
