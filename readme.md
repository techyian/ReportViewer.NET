# ReportViewer.NET

Welcome to ReportViewer.NET, a C#/.NET library which allows you to render SQL Server Reporting Services (SSRS) reports from within a web page.

The library works by parsing RDL files which you must register, along with DataSources, and the elements and expressions within the RDL file are then transformed into HTML. The library has a number of parsers which extract values from functions which are then used to build up a final expression, before being run through a C# interpreter called DynamicExpresso (https://github.com/dynamicexpresso/DynamicExpresso). 

**Please Note**: 

1. This is currently recognised as Alpha state software and is not guaranteed to work with your report. Although many of the common elements and expressions available via Report Builder have been included in ReportViewer.NET, there will be gaps in functionality and the rendered HTML is not intended to be a clone of that returned by the official Microsoft SSRS Report Web Viewer.

1. As ReportViewer.NET uses a C# interpreter for expressions, you must ensure that you are not performing any comparisons on non-compatible data types. More information in the Help/FAQ section below.

1. This project does not use any Microsoft proprietary code related to the original Report Viewer library and is my attempt at reverse engineering how RDL files should be processed without any knowledge of how the original Report Viewer library functions internally. If you want a full featured offering based on the original library but built for .NET Core 3.1 and later, check out reportviewercore https://github.com/lkosson/reportviewercore, but please note the repository owner's warning on the legality of distributing code with this package installed.

ReportViewer.NET is currently designed to target .NET 8.

## Dependencies

1. jQuery for the client side script bundled with this library. The script is not currently available as a module for importing, but I will eventually do this.

## How to use

1. Include the provided CSS in `assets/reportviewer.net.css` in your project.
2. Include the provided JS in `assets/reportviewer.net.js` in your project.
3. Create the HTML placeholder element and load the JS in your HTML file by adding the following: 

```
<div class="report-viewer">

</div>

<script type="text/javascript">
    var reportViewer = new ReportViewer('/api/Reports', 'My RDL file');

    reportViewer.postReportParameters();    
</script>

```

The `ReportViewer` function accepts two parameters: `apiPath` and `rdlName`, where `apiPath` references the name of your API path minus the endpoint itself. ReportViewer.NET expects two endpoints to be available on the given path: `GenerateParameters` and `GenerateReport`.

4. Read the remainder of this section to learn how to use ReportViewer.NET in your project.

ReportViewer.NET exposes the Interface `IReportHandler` and Class `ReportHandler` which are intended to be injected to your ASP.NET Controller via Dependency Injection. Please ensure that you register ReportViewer.NET using a Scoped/Transient lifetime - this library should not be registered with a Singleton lifetime.

The library exposes the Interface `IReportViewerController` to assist with giving you an example pattern for loading report parameters and rendering the report. It's recommended that your ASP.NET Core Controller inherits this Interface.

Example DI registration

**Program.cs**

```
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IReportHandler, ReportHandler>(serviceProvider =>
{
    var reportViewer = new ReportHandler();
    
    // The 'DATASOURCE NAME' should be registered using the same name as seen in the RDL.
    // Shared DataSources must be registered here.
    // The ReportViewer.NET library does not contact your Reporting Services Server to retrieve information.
    reportViewer.RegisterDataSource("DATASOURCE NAME", "CONNECTION STRING");
    
    // If you have sub-reports, make sure that the child reports are registered first before the parent 
    // and ensure the names match correctly.
    reportViewer.RegisterRdlFromFile("REPORT NAME", "FULL REPORT FILEPATH AND FILENAME");
    
    return reportViewer;
});
```

**HomeController.cs**

```
public class HomeController : Controller, IReportViewerController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IReportHandler _reportViewer;
    
    public HomeController(ILogger<HomeController> logger, IReportHandler reportViewer)
    {
        _logger = logger;
        _reportViewer = reportViewer;
    }
    
    // Default endpoints omitted for brevity.
    
    [HttpPost]
    public async Task<IActionResult> GenerateParameters([FromQuery] string rdl, [FromBody] ReportParameters userProvidedParameters)
    {
        // The 'LoadReport' method parses the report RDL's XML and initialises the C# object hierarchy.
        _reportViewer.LoadReport(rdl, userProvidedParameters);
        
        // PublishReportParameters will return HTML containing the parameters required for this report (if applicable)
        var paramHtml = await _reportViewer.PublishReportParameters(rdl, userProvidedParameters.Parameters);
    
        return Ok(paramHtml);
    }
    
    [HttpPost]
    public async Task<IActionResult> GenerateReport([FromQuery] string rdl, [FromBody] ReportParameters userProvidedParameters)
    {   
        // The 'LoadReport' method parses the report RDL's XML and initialises the C# object hierarchy.
        _reportViewer.LoadReport(rdl, userProvidedParameters);
        
        // The PublishReportOutput will return HTML containing the rendered report.
        var reportHtml = await _reportViewer.PublishReportOutput(rdl, userProvidedParameters);
    
        return Ok(reportHtml);
    }
}
```

## Tests

ReportViewer.NET will be tested against a suite of reports to ensure that the most common features available in Report Builder are working as expected. 

All reports available at the following GitHub link are tested against ReportViewer.NET: https://github.com/Apress/beg-sql-server-reporting-services. Due to licensing, the reports haven't been directly referenced by this repository, but please feel free to download the reports and try them out.

Additional reports are available in the `/test/reports/` directory of this repository and have been based off official Microsoft tutorials available here: https://learn.microsoft.com/en-us/sql/reporting-services/reporting-services-tutorials-ssrs?view=sql-server-ver16, and here: https://learn.microsoft.com/en-us/sql/reporting-services/report-builder-tutorials?view=sql-server-ver16.

## Help/FAQ

1. Q: When I have a tall element which spans multiple rows in my report, stacked members sharing the same row(s) aren't positioned correctly.
   
   A: Due to the way ReportViewer.NET calculates the position of elements, if you have a tall element which spans many rows, stacked members on the same row will not display as you may expect. Try wrapping stacked elements in a Rectangle. You will need to explicitly paste your elements into the Rectangle for this to work.

1. Q: The library returns "Could not parse, please check expression." when printing the value of an expression.

   A: Please examine your expression and ensure that you are not doing any comparisons on data types which are not compatible. ReportViewer.NET uses a C# interpreter to retrieve the final expression value and therefore will not allow implicit casting between two non-compatible data types, e.g. 1 == "1" which is allowed in VB.NET. If your expression looks sound and you're unable to determine the cause, please raise a ticket so I can investigate.

## Screenshots

### First Report - 
https://github.com/Apress/beg-sql-server-reporting-services/blob/master/Beginning%20SSRS/Learn%20SSRS/Beginning%20SSRS%20Chapter%202/Wizard%20Reports/First%20Report.rdl

![](https://github.com/techyian/ReportViewer.NET/blob/master/assets/images/First%20Report.PNG?raw=true)

### Matrix Report - 
https://github.com/Apress/beg-sql-server-reporting-services/blob/master/Beginning%20SSRS/Learn%20SSRS/Beginning%20SSRS%20Chapter%202/Wizard%20Reports/Matrix%20Report.rdl

![](https://github.com/techyian/ReportViewer.NET/blob/master/assets/images/Matrix%20Report.PNG?raw=true)

### Calculated Field - 
https://github.com/Apress/beg-sql-server-reporting-services/blob/master/Beginning%20SSRS/Learn%20SSRS/Beginning%20SSRS%20Chapter%204/Building%20Reports/Calculated%20Field.rdl

![](https://github.com/techyian/ReportViewer.NET/blob/master/assets/images/Calculated%20Field.PNG?raw=true)

### List Report - 
https://github.com/Apress/beg-sql-server-reporting-services/blob/master/Beginning%20SSRS/Learn%20SSRS/Beginning%20SSRS%20Chapter%204/Building%20Reports/List%20Report.rdl

![](https://github.com/techyian/ReportViewer.NET/blob/master/assets/images/List%20Report.PNG?raw=true)

### Sales by Territory 2 - 
https://github.com/Apress/beg-sql-server-reporting-services/blob/master/Beginning%20SSRS/Learn%20SSRS/Beginning%20SSRS%20Chapter%206/Dynamic%20Reports/Sales%20by%20Territory%202.rdl

![](https://github.com/techyian/ReportViewer.NET/blob/master/assets/images/Sales%20by%20Territory%202.PNG?raw=true)

### Sales by Territory Matrix -
https://github.com/Apress/beg-sql-server-reporting-services/blob/master/Beginning%20SSRS/Learn%20SSRS/Beginning%20SSRS%20Chapter%206/Dynamic%20Reports/Sales%20by%20Territory%20Matrix.rdl

![](https://github.com/techyian/ReportViewer.NET/blob/master/assets/images/Sales%20by%20Territory%20Matrix.PNG?raw=true)

### Visibility by Parameters - 
https://github.com/Apress/beg-sql-server-reporting-services/blob/master/Beginning%20SSRS/Learn%20SSRS/Beginning%20SSRS%20Chapter%206/Dynamic%20Reports/Visibility%20by%20Parameters.rdl

![](https://github.com/techyian/ReportViewer.NET/blob/master/assets/images/Visibility%20by%20Parameters%201.PNG?raw=true)

![](https://github.com/techyian/ReportViewer.NET/blob/master/assets/images/Visibility%20by%20Parameters%202.PNG?raw=true)

![](https://github.com/techyian/ReportViewer.NET/blob/master/assets/images/Visibility%20by%20Parameters%20all.PNG?raw=true)

## Element compatibility

| Element     | Status     | Comments                      |
| -------     | ------     | --------                      |
| Page Header | Complete   |                               |    
| Page Footer | Complete   |                               |    
| Textbox     | Complete   |                               |    
| Paragraph   | Complete   |                               |    
| Textrun     | Complete   |                               |    
| Style       | Partial    | Most common features are working. |    
| ActionInfo  | Partial    | Only hyperlinks currently working. |    
| Tablix      | Complete?  | Groups, sorting, TablixRowHierarchy, TablixColumnHierarchy appear working. Standard/matrix tables look pretty accurate from local testing.      |    
| Rectangle   | Complete   |                               |    
| Line        | Complete   |                               |    
| Image       | Partial    | TODO: Background images, external images.  |    
| Sub Report  | Complete   |                               |    
| Chart       | Not started|                               |
| Gauge       | Not started|                               |
| Map         | Not started|                               |
| Data Bar    | Not started|                               |
| Sparkline   | Not started|                               |
| Indicator   | Not started|                               |
| User Sort   | Not started|                               |
| Corner Row  | Complete   | Appears to be working as expected |


## Built-in field compatibility
| Field                          | Status        | Comments                   |
| -------                        | ------        | --------                   |
| ExecutionTime                  | Complete      |                            |
| Language                       | Complete      |                            |
| OverallPageNumber              | Not started   |                            |
| OverallTotalPages              | Not started   |                            |
| PageName                       | Not started   |                            |
| PageNumber                     | Not started   |                            |
| RenderFormat.IsInteractive     | Not started   |                            |
| RenderFormat.Name              | Not started   |                            |
| ReportFolder                   | Not started   |                            |
| ReportName                     | Complete      |                            |
| ReportServerUrl                | Not started   |                            |
| TotalPages                     | Not started   |                            |
| UserID                         | Not started   |                            |

## Operators compatibility
| Group           | Operator    | Status        | Comments                   |
| -----           | -------     | ------        | --------                   |
| Arithmetic      | ^           | Complete      |                            |
| Arithmetic      | *           | Complete      |                            |
| Arithmetic      | /           | Complete      |                            |
| Arithmetic      | \           | Not started   |                            |
| Arithmetic      | Mod         | Complete      |                            |
| Arithmetic      | +           | Complete      |                            |
| Arithmetic      | -           | Complete      |                            |
| Comparison      | <           | Complete      |                            |
| Comparison      | <=          | Complete      |                            |
| Comparison      | >           | Complete      |                            |
| Comparison      | >=          | Complete      |                            |
| Comparison      | =           | Complete      |                            |
| Comparison      | <>          | Complete      |                            |
| Comparison      | Like        | Not started   |                            |
| Comparison      | Is          | Not started   |                            |
| Concatenation   | &           | Complete      |                            |
| Concatenation   | +           | Complete      |                            |
| Logical/Bitwise | And         | Complete      |                            |
| Logical/Bitwise | Not         | Complete      |                            |
| Logical/Bitwise | Or          | Complete      |                            |
| Logical/Bitwise | Xor         | Complete      |                            |
| Logical/Bitwise | AndAlso     | Partial       | Using same logic as And    |
| Logical/Bitwise | OrElse      | Partial       | Using same logic as Or     |
| Bitshift        | >>          | Not started   |                            |
| Bitshift        | <<          | Not started   |                            |



## Common functions compatibility

| Group | Expression     | Status        | Comments                   |
| ----- | -------        | ------        | --------                   |
|       | Fields         | Complete      |                            |
|       | Parameters     | Complete      |                            |

### Text
| Group | Expression     | Status        | Comments                   |
| ----- | -------        | ------        | --------                   |
| Text  | Asc            | Complete      |                            |
| Text  | AscW           | Complete      |                            |
| Text  | Chr            | Complete      |                            |
| Text  | ChrW           | Complete      |                            |
| Text  | Filter         | Not started   |                            |
| Text  | Format         | Complete      | Culture specific DateTime, Int and Double format. Culture independent double parsing. Added code to handle VB specific formatters e.g. Percentage, Scientific.                             |
| Text  | FormatCurrency | Complete      |                            |
| Text  | FormatDateTime | Complete      |                            |
| Text  | FormatNumber   | Complete      |                            |
| Text  | FormatPercent  | Complete      |                            |
| Text  | GetChar        | Complete      |                            |
| Text  | InStr          | Complete      |                            |
| Text  | InStrRev       | Complete      |                            |
| Text  | Join           | Not started   |                            |
| Text  | LCase          | Not started   |                            |
| Text  | Left           | Complete      |                            |
| Text  | Len            | Not started   |                            |
| Text  | LSet           | Not started   |                            |
| Text  | LTrim          | Not started   |                            |
| Text  | Mid            | Not started   |                            |
| Text  | Replace        | Not started   |                            |
| Text  | Right          | Not started   |                            |
| Text  | RSet           | Not started   |                            |
| Text  | RTrim          | Not started   |                            |
| Text  | Space          | Not started   |                            |
| Text  | Split          | Not started   |                            |
| Text  | StrComp        | Not started   |                            |
| Text  | StrConv        | Not started   |                            |
| Text  | StrDup         | Not started   |                            |
| Text  | StrRev         | Not started   |                            |
| Text  | Trim           | Not started   |                            |
| Text  | UCase          | Not started   |                            |

### Date & Time

| Group | Expression     | Status        | Comments                   |
| ----- | -------        | ------        | --------                   |
| Date & Time  | CDate           | Complete      |                            |
| Date & Time  | DateAdd         | Complete      |                            |
| Date & Time  | DateDiff        | Complete      |                            |
| Date & Time  | DatePart        | Partial       | Not yet tested with all time intervals                            |
| Date & Time  | DateSerial      | Complete      |                            |
| Date & Time  | DateString      | Complete      |                            |
| Date & Time  | DateValue       | Complete      |                            |
| Date & Time  | Day             | Complete      |                            |
| Date & Time  | FormatDateTime  | Complete      |                            |
| Date & Time  | Hour            | Complete      |                            |
| Date & Time  | Minute          | Complete      |                            |
| Date & Time  | Month           | Complete      |                            |
| Date & Time  | MonthName       | Complete      |                            |
| Date & Time  | Now             | Complete      |                            |
| Date & Time  | Second          | Complete      |                            |
| Date & Time  | TimeOfDay       | Complete      |                            |
| Date & Time  | Timer           | Complete      |                            |
| Date & Time  | TimeSerial      | Complete      |                            |
| Date & Time  | TimeString      | Complete      |                            |
| Date & Time  | TimeValue       | Complete      |                            |
| Date & Time  | Today           | Complete      |                            |
| Date & Time  | Weekday         | Complete      |                            |
| Date & Time  | WeekdayName     | Complete      |                            |
| Date & Time  | Year            | Complete      |                            |

### Math

| Group | Expression      | Status        | Comments                   |
| ----- | -------         | ------        | --------                   |
| Math  | Abs             | Not started   |                            |
| Math  | Acos            | Not started   |                            |
| Math  | Asin            | Not started   |                            |
| Math  | Atan            | Not started   |                            |
| Math  | Atan2           | Not started   |                            |
| Math  | BigMul          | Not started   |                            |
| Math  | Ceiling         | Not started   |                            |
| Math  | Cos             | Not started   |                            |
| Math  | Cosh            | Not started   |                            |
| Math  | Exp             | Not started   |                            |
| Math  | Fix             | Not started   |                            |
| Math  | Floor           | Not started   |                            |
| Math  | Int             | Not started   |                            |
| Math  | Log             | Not started   |                            |
| Math  | Log10           | Not started   |                            |
| Math  | Max             | Not started   |                            |
| Math  | Min             | Not started   |                            |
| Math  | Pow             | Not started   |                            |
| Math  | Rnd             | Not started   |                            |
| Math  | Round           | Not started   |                            |
| Math  | Sign            | Not started   |                            |
| Math  | Sin             | Not started   |                            |
| Math  | Sinh            | Not started   |                            |
| Math  | Sqrt            | Not started   |                            |
| Math  | Tan             | Not started   |                            |
| Math  | Tanh            | Not started   |                            |

### Inspection

| Group         | Expression        | Status        | Comments                   |
| -----         | -------           | ------        | --------                   |
| Inspection    | IsArray           | Not started   |                            |
| Inspection    | IsDate            | Not started   |                            |
| Inspection    | IsNothing         | Complete      |                            |
| Inspection    | IsNumeric         | Not started   |                            |

### Program Flow

| Group         | Expression        | Status        | Comments                   |
| -----         | -------           | ------        | --------                   |
| Program Flow  | Choose            | Not started   |                            |
| Program Flow  | IIf               | Complete      |                            |
| Program Flow  | Switch            | Not started   |                            |

### Aggregate 

| Group         | Expression        | Status        | Comments                   |
| -----         | -------           | ------        | --------                   |
| Aggregate     | Avg               | Not started   |                            |
| Aggregate     | Count             | Partial       | TODO: Handle other count expressions not using fields?? |
| Aggregate     | CountDistinct     | Partial       | As above                   |
| Aggregate     | CountRows         | Not started   |                            |
| Aggregate     | First             | Partial       | TODO: Filtering on field value e.g. =First(Fields!MiddleInitial.Value = "P") TODO: Filtering on field value by parameter value e.g. =First(Fields!MiddleInitial.Value = Parameters!MiddleInitial.Value(0))           |
| Aggregate     | Last              | Not started   |                            |
| Aggregate     | Max               | Not started   |                            |
| Aggregate     | Min               | Not started   |                            |
| Aggregate     | StDev             | Not started   |                            |
| Aggregate     | StDevP            | Not started   |                            |
| Aggregate     | StDev             | Not started   |                            |
| Aggregate     | Sum               | Partial       | TODO: Handle other sum expressions not using fields?? |
| Aggregate     | Var               | Not started   |                            |
| Aggregate     | VarP              | Not started   |                            |
| Aggregate     | RunningValue      | Not started   |                            |
| Aggregate     | Aggregate         | Not started   |                            |

### Financial
| Group         | Expression        | Status        | Comments                   |
| -----         | -------           | ------        | --------                   |
| Financial     | DDB               | Not started   |                            |
| Financial     | FV                | Not started   |                            |
| Financial     | IPmt              | Not started   |                            |
| Financial     | NPer              | Not started   |                            |
| Financial     | Pmt               | Not started   |                            |
| Financial     | PPmt              | Not started   |                            |
| Financial     | PV                | Not started   |                            |
| Financial     | Rate              | Not started   |                            |
| Financial     | SLN               | Not started   |                            |
| Financial     | SYD               | Not started   |                            |

### Conversion

| Group         | Expression        | Status        | Comments                   |
| -----         | -------           | ------        | --------                   |
| Conversion    | CBool             | Complete      |                            |
| Conversion    | CByte             | Not started   |                            |
| Conversion    | CChar             | Complete      |                            |
| Conversion    | CDate             | Complete      |                            |
| Conversion    | CDbl              | Not started   |                            |
| Conversion    | CDec              | Not started   |                            |
| Conversion    | CInt              | Complete      |                            |
| Conversion    | CLng              | Not started   |                            |
| Conversion    | CObj              | Not started   |                            |
| Conversion    | CShort            | Not started   |                            |
| Conversion    | CSng              | Not started   |                            |
| Conversion    | CStr              | Not started   |                            |
| Conversion    | Fix               | Not started   |                            |
| Conversion    | Hex               | Not started   |                            |
| Conversion    | Int               | Not started   |                            |
| Conversion    | Oct               | Not started   |                            |
| Conversion    | Str               | Not started   |                            |
| Conversion    | Val               | Not started   |                            |

### Misc

| Group         | Expression        | Status        | Comments                   |
| -----         | -------           | ------        | --------                   |
| Misc          | InScope           | Not started   |                            |
| Misc          | Level             | Not started   |                            |
| Misc          | Lookup            | Not started   |                            |
| Misc          | LookupSet         | Not started   |                            |
| Misc          | MultiLookup       | Not started   |                            |
| Misc          | Previous          | Not started   |                            |
| Misc          | RowNumber         | Partial       | Not tested with named scope |



## License

ReportViewer.NET is made available under the MIT license.