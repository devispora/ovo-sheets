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

    private async Task<ActionResult<string>> CreateNewSheet(string templateId, SheetDetails sheetDetails)
    {
        var emails = string.Join(";", sheetDetails.Emails);
        var date = sheetDetails.Start.Date.ToString("yyyy-MM-dd");
        var time = sheetDetails.Start.ToString("HH:mm");
        
        var newFile = await _driveService.Files.Copy(new File
        {
            Name = $"{date} [{sheetDetails.GroupName}]"
        }, templateId).ExecuteAsync();

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

        switch (sheetDetails.SharedStatus)
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

        return Ok(newFileId);
    }

    [HttpPost]
    [Route("new")]
    public async Task<ActionResult<string>> CreateSheet([FromBody] SheetDetails sheetDetails)
    {
        var ovoTemplate = _configuration["OvO:TemplateId"];
        var obsTemplate = _configuration["Obs:TemplateId"];

        switch (sheetDetails.ReservationType)
        {
            case ReservationType.BasicJaegerAccounts:
            {
                return await CreateNewSheet(ovoTemplate, sheetDetails);

                break;
            }
            case ReservationType.ObserverAccounts:
            {
                return await CreateNewSheet(obsTemplate, sheetDetails);

                break;
            }
        }

        return NoContent();
    }
}