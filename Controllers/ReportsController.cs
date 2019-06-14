using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FastReport;
using FastReport.Export.Image;
using FastReport.Export.Html;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using FRWeb.Models;
using System.Web.Hosting;
using System.Data;
using System.IO;
using System.Net.Http.Headers;

namespace FRWeb.Controllers
{
    // The class of parameters in the query
    public class ReportQuery
    {
        // Format of resulting report: png, pdf, html
        public string Format { get; set; }
        // Value of "Parameter" variable in report
        public string Parameter { get; set; }
        // Enable Inline preview in browser (generates "inline" or "attachment")
        public bool Inline { get; set; }
    }

    public class ReportsController : ApiController
    { // Reports list
        Reports[] reportItems = new Reports[]
        {
         new Reports { Id = 1, ReportName = "Box.frx" },
         new Reports { Id = 2, ReportName = "Barcode.frx" }
        };

        // Get reports list
        public IEnumerable<Reports> GetAllReports()
        {
            return reportItems;
        }

        // Get report on ID from request
        public HttpResponseMessage GetReportById(int id, [FromUri] ReportQuery query)
        {
            // Find report
            Reports reportItem = reportItems.FirstOrDefault((p) => p.Id == id);
            if (reportItem != null)
            {
                string reportPath = HostingEnvironment.MapPath("~/App_Data/" + reportItem.ReportName);
                string dataPath = HostingEnvironment.MapPath("~/App_Data/nwind-employees.xml");
                MemoryStream stream = new MemoryStream();
                try
                {
                    using (DataSet dataSet = new DataSet())
                    {
                        //Fill data source
                        dataSet.ReadXml(dataPath);
                        //Enable web mode
                        Config.WebMode = true;
                        using (Report report = new Report())
                        {
                            report.Load(reportPath); //Load report
                            report.RegisterData(dataSet, "NorthWind"); //Register Data in report
                            if (query.Parameter != null)
                            {
                                report.SetParameterValue("Parameter", query.Parameter); // Set the value of the parameter in the report. The value we take from the URL
                            }

                            // Two phases of preparation to exclude the display of any dialogs
                            report.PreparePhase1();
                            report.PreparePhase2();

                            if (query.Format == "pdf")
                            {
                                //Export in PDF
                                PDFSimpleExport pdf = new PDFSimpleExport();
                                // We use the flow to store the report, so as not to produce files
                                report.Export(pdf, stream);
                            }
                            else if (query.Format == "html")
                            {
                                // Export in HTML
                                HTMLExport html = new HTMLExport();
                                html.SinglePage = true;
                                html.Navigator = false;
                                html.EmbedPictures = true;
                                report.Export(html, stream);
                            }
                            else
                            {
                                // Export in picture
                                ImageExport img = new ImageExport();
                                img.ImageFormat = ImageExportFormat.Png;
                                img.SeparateFiles = false;
                                img.ResolutionX = 96;
                                img.ResolutionY = 96;
                                report.Export(img, stream);
                                query.Format = "png";
                            }
                        }
                    }
                    // Create result variable
                    HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(stream.ToArray())
                    };
                    stream.Dispose();

                    result.Content.Headers.ContentDisposition =
                    new System.Net.Http.Headers.ContentDispositionHeaderValue(query.Inline ? "inline" : "attachment")
                    {
     // Specify the file extension depending on the type of export FileName = String.Concat(Path.GetFileNameWithoutExtension(reportPath), ".", query.Format)
 };
                    // Determine the type of content for the browser
                    result.Content.Headers.ContentType =
                     new MediaTypeHeaderValue("application/" + query.Format);
                    return result;
                }
                // We handle exceptions
                catch
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
            }
            else
                return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}