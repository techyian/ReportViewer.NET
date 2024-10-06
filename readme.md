# ReportViewer.NET

Welcome to ReportViewer.NET, a C#/.NET library which allows you to render SQL Server Reporting Services (SSRS) reports from within a web page.

The library works by parsing RDL files which you must register, along with DataSources, and the elements and expressions within the RDL file are then transformed into HTML.

**Please Note**: This is currently recognised as Alpha state software and is not guaranteed to work with your report. Although many of the common elements and expressions available via Report Builder have been included in ReportViewer.NET, there will be gaps in functionality and the rendered HTML is not intended to be a clone of that returned by the official Microsoft SSRS Report Web Viewer.

ReportViewer.NET is currently designed to target .NET 8.

## How to use

ReportViewer.NET exposes the Interface `IReportHandler` and Class `ReportHandler` which are intended to be injected to your ASP.NET Controller via Dependency Injection. Please ensure that you register ReportViewer.NET using a Scoped/Transient lifetime - this library should not be registered with a Singleton lifetime.

The library exposes the Interface `IReportViewerController` to assist with giving you an example pattern for loading report parameters and rendering the report. It's recommended that your ASP.NET Core Controller inherits this Interface.

The library contains sample JavaScript which manages client side interaction with the library and can currently be found in `src\ReportViewer.NET.Web\wwwroot\js\site.js`. I will make sure this is made available in a more flexible fashion in the future.

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
    public async Task<IActionResult> ParameterViewer([FromQuery] string rdl, [FromBody] ReportParameters userProvidedParameters)
    {
        // The 'LoadReport' method parses the report RDL's XML and initialises the C# object hierarchy.
        _reportViewer.LoadReport(rdl, userProvidedParameters);
        
        // PublishReportParameters will return HTML containing the parameters required for this report (if applicable)
        var paramHtml = await _reportViewer.PublishReportParameters(rdl, userProvidedParameters.Parameters);
    
        return Ok(paramHtml);
    }
    
    [HttpPost]
    public async Task<IActionResult> ReportViewer([FromQuery] string rdl, [FromBody] ReportParameters userProvidedParameters)
    {   
        // The 'LoadReport' method parses the report RDL's XML and initialises the C# object hierarchy.
        _reportViewer.LoadReport(rdl, userProvidedParameters);
        
        // The PublishReportOutput will return HTML containing the rendered report.
        var reportHtml = await _reportViewer.PublishReportOutput(rdl, userProvidedParameters);
    
        return Ok(reportHtml);
    }
}
```

## Element compatibility

| Element     | Status     | Comments                   |
| -------     | ------     | --------                   |
| Page Header | Complete   |                            |    
| Page Footer | Complete   |                            |    
| Textbox     | Complete   |                            |    
| Paragraph   | Complete   |                            |    
| Textrun     | Complete   |                            |    
| Style       | Partial    | Most common features are working. |    
| ActionInfo  | Partial    | Only hyperlinks currently working. |    
| Tablix      | Complete?  | Groups, sorting, TablixRowHierarchy, TablixColumnHierarchy appear working. Standard/matrix tables look pretty accurate from local testing.      |    

## Built-in field compatibility
| Field                          | Status        | Comments                   |
| -------                        | ------        | --------                   |
| ExecutionTime                  | Complete      |                            |
| Language                       | Not started   |                            |
| OverallPageNumber              | Not started   |                            |
| OverallTotalPages              | Not started   |                            |
| PageName                       | Not started   |                            |
| PageNumber                     | Not started   |                            |
| RenderFormat.IsInteractive     | Not started   |                            |
| RenderFormat.Name              | Not started   |                            |
| ReportFolder                   | Not started   |                            |
| ReportName                     | Not started   |                            |
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
| Text  | Asc            | Not started   |                            |
| Text  | AscW           | Not started   |                            |
| Text  | Asc            | Not started   |                            |
| Text  | Chr            | Not started   |                            |
| Text  | ChrW           | Not started   |                            |
| Text  | Filter         | Not started   |                            |
| Text  | Format         | Not started   |                            |
| Text  | FormatCurrency | Complete      |                            |
| Text  | FormatDateTime | Not started   |                            |
| Text  | FormatNumber   | Not started   |                            |
| Text  | FormatPercent  | Not started   |                            |
| Text  | GetChar        | Not started   |                            |
| Text  | InStr          | Not started   |                            |
| Text  | InStrRev       | Not started   |                            |
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
| Date & Time  | CDate           | Not started   |                            |
| Date & Time  | DateAdd         | Not started   |                            |
| Date & Time  | DateDiff        | Not started   |                            |
| Date & Time  | DatePart        | Not started   |                            |
| Date & Time  | DateSerial      | Not started   |                            |
| Date & Time  | DateString      | Not started   |                            |
| Date & Time  | DateValue       | Not started   |                            |
| Date & Time  | Day             | Not started   |                            |
| Date & Time  | FormatDateTime  | Not started   |                            |
| Date & Time  | Hour            | Not started   |                            |
| Date & Time  | Minute          | Not started   |                            |
| Date & Time  | Month           | Not started   |                            |
| Date & Time  | MonthName       | Complete      |                            |
| Date & Time  | Now             | Not started   |                            |
| Date & Time  | Second          | Not started   |                            |
| Date & Time  | TimeOfDay       | Not started   |                            |
| Date & Time  | Timer           | Not started   |                            |
| Date & Time  | TimeSerial      | Not started   |                            |
| Date & Time  | TimeString      | Not started   |                            |
| Date & Time  | TimeValue       | Not started   |                            |
| Date & Time  | Today           | Not started   |                            |
| Date & Time  | Weekday         | Not started   |                            |
| Date & Time  | WeekdayName     | Not started   |                            |
| Date & Time  | Year            | Not started   |                            |
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
| Inspection    | IsArray           | Not started   |                            |
| Inspection    | IsDate            | Not started   |                            |
| Inspection    | IsNothing         | Complete      |                            |
| Inspection    | IsNumeric         | Not started   |                            |
| Program Flow  | Choose            | Not started   |                            |
| Program Flow  | IIf               | Complete      |                            |
| Program Flow  | Switch            | Not started   |                            |
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
| Conversion    | CBool             | Not started   |                            |
| Conversion    | CByte             | Not started   |                            |
| Conversion    | CChar             | Not started   |                            |
| Conversion    | CDate             | Not started   |                            |
| Conversion    | CDbl              | Not started   |                            |
| Conversion    | CDec              | Not started   |                            |
| Conversion    | CDec              | Not started   |                            |
| Conversion    | CInt              | Not started   |                            |
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
| Misc          | InScope           | Not started   |                            |
| Misc          | Level             | Not started   |                            |
| Misc          | Lookup            | Not started   |                            |
| Misc          | LookupSet         | Not started   |                            |
| Misc          | MultiLookup       | Not started   |                            |
| Misc          | Previous          | Not started   |                            |
| Misc          | RowNumber         | Not started   |                            |
