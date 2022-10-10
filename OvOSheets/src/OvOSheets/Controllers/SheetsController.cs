using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.AspNetCore.Mvc;
using OvOSheets.Models.Api;
using File = Google.Apis.Drive.v3.Data.File;

namespace OvOSheets.Controllers;

[ApiController]
[Route("sheets")]
public class SheetsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly DriveService _driveService;
    private readonly SheetsService _sheetsService;
    private ILogger<SheetsController> _logger;

    public SheetsController(ILogger<SheetsController> logger, DriveService driveService, SheetsService sheetsService,
        IConfiguration configuration)
    {
        _logger = logger;
        _driveService = driveService;
        _sheetsService = sheetsService;
        _configuration = configuration;
    }

    private async Task<ActionResult<string>> CreateNewSheet(string templateId, CreateSheet options)
    {
        var emails = string.Join(";", options.Emails);
        var date = options.Start.Date.ToString("yyyy-MM-dd");
        var time = options.Start.ToString("HH:mm");

        var templateFile = await _driveService.Files.Get(templateId).ExecuteAsync();

        if (templateFile == null) return Problem("Template not found");

        var newFile = await _driveService.Files.Copy(new File(), templateFile.Id).ExecuteAsync();

        if (newFile == null) return Problem("Failed copying template");

        var newFileId = newFile.Id;

        var data = new List<ValueRange>
        {
            new()
            {
                MajorDimension = "ROWS",
                Range = "Sheet1!B1:D1",
                Values = new List<IList<object>> { new List<object> { emails } }
            },
            new()
            {
                MajorDimension = "ROWS",
                Range = "Sheet1!B2:D2",
                Values = new List<IList<object>> { new List<object> { date } }
            },
            new()
            {
                MajorDimension = "ROWS",
                Range = "Sheet1!B3:C3",
                Values = new List<IList<object>> { new List<object> { time } }
            }
        };

        switch (options.SharedStatus)
        {
            case SharedStatus.ManuallyShared:
            {
                data.Add(new ValueRange
                {
                    MajorDimension = "ROWS",
                    Range = "Sheet1!B4:D4",
                    Values = new List<IList<object>> { new List<object> { "Manually Shared" } }
                });

                break;
            }
            case SharedStatus.NotReady:
            {
                data.Add(new ValueRange
                {
                    MajorDimension = "ROWS",
                    Range = "Sheet1!B4:D4",
                    Values = new List<IList<object>> { new List<object> { "Not Ready" } }
                });

                break;
            }
        }

        await _sheetsService.Spreadsheets.Values.BatchUpdate(new BatchUpdateValuesRequest
        {
            Data = data,
            ValueInputOption = "USER_ENTERED"
        }, newFile.Id).ExecuteAsync();

        await _driveService.Files.Update(new File
        {
            Name = $"{date} [{options.GroupName}]"
        }, newFileId).ExecuteAsync();

        return Ok(newFileId);
    }

    [HttpPost]
    [Route("new")]
    public async Task<ActionResult<string>> CreateSheet([FromBody] CreateSheet options)
    {
        var ovoTemplate = _configuration["OvO:TemplateId"];
        var obsTemplate = _configuration["Obs:TemplateId"];

        switch (options.ReservationType)
        {
            case ReservationType.BasicJaegerAccounts:
            {
                return await CreateNewSheet(ovoTemplate, options);

                break;
            }
            case ReservationType.ObserverAccounts:
            {
                return await CreateNewSheet(obsTemplate, options);

                break;
            }
        }

        return NoContent();
    }
}