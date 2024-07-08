using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using MarketingCodingAssignment.Models;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Lucene.Net.Analysis.En;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.QueryParsers.Classic;

namespace MarketingCodingAssignment.Services
{
    public class SearchEngine
    {
        // The code below is roughly based on sample code from: https://lucenenet.apache.org/
        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        private readonly string _indexPath;

        public SearchEngine()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            _indexPath = Path.Combine(basePath, "index");
        }

        public List<FilmCsvRecord> ReadFilmsFromCsv()
        {
            List<FilmCsvRecord> records = new();
            string filePath = $"{System.IO.Directory.GetCurrentDirectory()}{@"\wwwroot\csv"}" + "\\" + "FilmsInfo.csv";
            using (StreamReader reader = new(filePath))
            using (CsvReader csv = new(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                records = csv.GetRecords<FilmCsvRecord>().ToList();

            }
            using (StreamReader r = new(filePath))
            {
                string csvFileText = r.ReadToEnd();
            }
            return records;
        }

        // Read the data from the csv and feed it into the lucene index
        public void PopulateIndexFromCsv()
        {
            // Get the list of films from the csv file
            var csvFilms = ReadFilmsFromCsv();

            // Convert to Lucene format
            List<FilmLuceneRecord> luceneFilms = csvFilms.Select(x => new FilmLuceneRecord
            {
                Id = x.Id,
                Title = x.Title,
                Overview = x.Overview,
                Runtime = int.TryParse(x.Runtime, out int parsedRuntime) ? parsedRuntime : 0,
                Tagline = x.Tagline,
                Revenue = long.TryParse(x.Revenue, out long parsedRevenue) ? parsedRevenue : 0,
                VoteAverage = double.TryParse(x.VoteAverage, out double parsedVoteAverage) ? parsedVoteAverage : 0,
                ReleaseDate = DateTime.TryParse(x.ReleaseDate, out DateTime parsedReleaseDate) ? parsedReleaseDate : null
            }).ToList();

            // Write the records to the lucene index
            PopulateIndex(luceneFilms);

            return;
        }

        public void PopulateIndex(List<FilmLuceneRecord> films)
        {
            // Construct a machine-independent path for the index
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string indexPath = Path.Combine(basePath, "index");
            using FSDirectory dir = FSDirectory.Open(indexPath);

            // Create an analyzer to process the text
            StandardAnalyzer analyzer = new(AppLuceneVersion);

            // Create an index writer
            IndexWriterConfig indexConfig = new(AppLuceneVersion, analyzer);
            using IndexWriter writer = new(dir, indexConfig);

            //Add to the index
            foreach (var film in films)
            {
                Document doc = new()
                {
                    new StringField("Id", film.Id, Field.Store.YES),
                    new TextField("Title", film.Title, Field.Store.YES),
                    new TextField("Overview", film.Overview, Field.Store.YES),
                    new Int32Field("Runtime", film.Runtime, Field.Store.YES),
                    new TextField("Tagline", film.Tagline, Field.Store.YES),
                    new Int64Field("Revenue", film.Revenue ?? 0, Field.Store.YES),
                    new DoubleField("VoteAverage", film.VoteAverage ?? 0.0, Field.Store.YES),
                    new TextField("CombinedText", film.Title + " " + film.Tagline + " " + film.Overview, Field.Store.NO),
                    new StringField("ReleaseDate", film.ReleaseDate.HasValue ? film.ReleaseDate.Value.ToString("yyyyMMdd") : string.Empty, Field.Store.YES)

                };
                writer.AddDocument(doc);
            }

            writer.Flush(triggerMerge: false, applyAllDeletes: false);
            writer.Commit();

           return;
        }

        public void DeleteIndex()
        {
            // Delete everything from the index
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string indexPath = Path.Combine(basePath, "index");
            using FSDirectory dir = FSDirectory.Open(indexPath);
            StandardAnalyzer analyzer = new(AppLuceneVersion);
            IndexWriterConfig indexConfig = new(AppLuceneVersion, analyzer);
            using IndexWriter writer = new(dir, indexConfig);
            writer.DeleteAll();
            writer.Commit();
            return;
        }

        public SearchResultsViewModel Search(string searchString, int startPage, int rowsPerPage, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum, DateTime? releaseDateStart, DateTime? releaseDateEnd)
        {
            // Debugging logs
            Console.WriteLine($"Search Params - searchString: {searchString}, startPage: {startPage}, rowsPerPage: {rowsPerPage}, durationMinimum: {durationMinimum}, durationMaximum: {durationMaximum}, voteAverageMinimum: {voteAverageMinimum}, releaseDateStart: {releaseDateStart}, releaseDateEnd: {releaseDateEnd}");

            // Construct a machine-independent path for the index
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string indexPath = Path.Combine(basePath, "index");
            using FSDirectory dir = FSDirectory.Open(indexPath);
            using DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new(reader);

            int hitsLimit = 1000;
            TopScoreDocCollector collector = TopScoreDocCollector.Create(hitsLimit, true);

            var query = this.GetLuceneQuery(searchString, durationMinimum, durationMaximum, voteAverageMinimum, releaseDateStart, releaseDateEnd);

            searcher.Search(query, collector);

            int startIndex = startPage * rowsPerPage;
            TopDocs hits = collector.GetTopDocs(startIndex, rowsPerPage);
            ScoreDoc[] scoreDocs = hits.ScoreDocs;

            List<FilmLuceneRecord> films = new();
            foreach (ScoreDoc? hit in scoreDocs)
            {
                Document foundDoc = searcher.Doc(hit.Doc);
                FilmLuceneRecord film = new()
                {
                    Id = foundDoc.Get("Id")?.ToString(),
                    Title = foundDoc.Get("Title")?.ToString(),
                    Overview = foundDoc.Get("Overview")?.ToString(),
                    Runtime = int.TryParse(foundDoc.Get("Runtime"), out int parsedRuntime) ? parsedRuntime : 0,
                    Tagline = foundDoc.Get("Tagline")?.ToString(),
                    Revenue = long.TryParse(foundDoc.Get("Revenue"), out long parsedRevenue) ? parsedRevenue : 0,
                    VoteAverage = double.TryParse(foundDoc.Get("VoteAverage"), out double parsedVoteAverage) ? parsedVoteAverage : 0.0,
                    ReleaseDate = DateTime.TryParseExact(foundDoc.Get("ReleaseDate"), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate) ? parsedDate : null,
                    Score = hit.Score
                };
                films.Add(film);
            }

            SearchResultsViewModel searchResults = new()
            {
                RecordsCount = hits.TotalHits,
                Films = films.ToList()
            };

            return searchResults;
        }

        private Query GetLuceneQuery(string searchString, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum, DateTime? releaseDateStart, DateTime? releaseDateEnd)
        {
            BooleanQuery bq = new();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var pq = new MultiPhraseQuery();
                foreach (var word in searchString.Split(" ").Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    if (!EnglishAnalyzer.DefaultStopSet.Contains(word))
                    {
                        pq.Add(new Term("CombinedText", word.ToLowerInvariant()));
                    }
                }
                bq.Add(pq, Occur.MUST);
            }

            if (durationMinimum.HasValue || durationMaximum.HasValue)
            {
                Query rq = NumericRangeQuery.NewInt32Range("Runtime", durationMinimum, durationMaximum, true, true);
                bq.Add(rq, Occur.MUST);
            }

            if (voteAverageMinimum.HasValue)
            {
                Query vaq = NumericRangeQuery.NewDoubleRange("VoteAverage", voteAverageMinimum, 10.0, true, true);
                bq.Add(vaq, Occur.MUST);
            }

            if (releaseDateStart.HasValue)
            {
                string startDateStr = releaseDateStart.Value.ToString("yyyy-MM-dd");
                string endDateStr = releaseDateEnd.HasValue ? releaseDateEnd.Value.ToString("yyyy-MM-dd") : "9999-12-31";
                bq.Add(new TermRangeQuery("ReleaseDate", new BytesRef(startDateStr), new BytesRef(endDateStr), true, true), Occur.MUST);
            }

            return bq;
        }

        public List<string> GetSuggestions(string term)
        {
            using FSDirectory dir = FSDirectory.Open(new DirectoryInfo(_indexPath));
            using DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new IndexSearcher(reader);

            var parser = new QueryParser(AppLuceneVersion, "CombinedText", new StandardAnalyzer(AppLuceneVersion));
            Query query = parser.Parse(term + "*");

            TopDocs topDocs = searcher.Search(query, 5);

            List<string> suggestions = new();
            foreach (ScoreDoc hit in topDocs.ScoreDocs)
            {
                Document doc = searcher.Doc(hit.Doc);
                string title = doc.Get("Title");
                if (!suggestions.Contains(title))
                {
                    suggestions.Add(title);
                }
            }

            return suggestions;
        }

        public string GetCorrectedTerm(string term)
        {
            return term; 
        }

    }
}

