// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
using WindowsUpdateLib;

List<CTAC> CTACs = new()
{
    //new(OSSkuId.Professional, "10.0.15063.534", MachineType.amd64, "WIS", "", "CB", "rs2_release", "Production", false),
    new(OSSkuId.Professional, "10.0.16299.15",   MachineType.amd64, "Retail", "", "CB", "rs3_release",  "Production", true),  //RS3
    new(OSSkuId.Professional, "10.0.17134.1",    MachineType.amd64, "Retail", "", "CB", "rs4_release",  "Production", true),  //RS4
    new(OSSkuId.Professional, "10.0.17763.1217", MachineType.amd64, "Retail", "", "CB", "rs5_release",  "Production", true),  //RS5
    new(OSSkuId.Professional, "10.0.18362.836",  MachineType.amd64, "Retail", "", "CB", "19h1_release", "Production", true),  //TI
    new(OSSkuId.Professional, "10.0.19041.200",  MachineType.amd64, "Retail", "", "CB", "vb_release",   "Production", false), //VB
};
String token = String.Empty;

for (int i = 0; i < CTACs.Count; i++) {
    Console.WriteLine(CTACs[i].CTACParams());

    for (int hack = 0; hack <= 1; hack++) {
        if (hack == 1) {
            //CTACs[i].DeviceAttributes += "&TargetReleaseVersionInfo=1703";
            CTACs[i].DeviceAttributes += "&BlockFeatureUpdates=1";
        }

        List<UpdateData> data = (await FE3Handler.GetUpdates(null, CTACs[i], token, FileExchangeV3UpdateFilter.ProductRelease)).ToList();
        foreach (UpdateData update in data) {
            string buildStr = await update.GetBuildStringAsync();
            if (string.IsNullOrEmpty(buildStr)) {
                buildStr = update.Xml.LocalizedProperties.Title;
            }
            Console.WriteLine($"- {buildStr}");
            await AddNewCTAC(update);
        }
    }
}

async Task AddNewCTAC(UpdateData update) {
    string buildStr = await update.GetBuildStringAsync();
    if (string.IsNullOrEmpty(buildStr)) {
        buildStr = update.Xml.LocalizedProperties.Title;
    }
    String[] parts = buildStr.Split(" ", 2);

    OSSkuId ReportingSku = update.CTAC.GetReportingSku();
    String ReportingVersion = parts[0];
    MachineType MachineType = update.CTAC.GetMachineType();
    String FlightRing = update.CTAC.GetFlightRing();
    String FlightingBranchName = update.CTAC.GetFlightingBranchName();
    String BranchReadinessLevel = update.CTAC.GetBranchReadinessLevel();
    String CurrentBranch = Regex.Match(parts[1], @"^\((?'branch'[^_]+_[^_\.]+)[_\.].+\)$").Groups["branch"].Value;
    String ReleaseType = update.CTAC.GetReleaseType();
    Boolean SyncCurrentVersionOnly = update.CTAC.SyncCurrentVersionOnly;
    for (int i = 0; i < CTACs.Count; i++) {
        if (
            CTACs[i].GetReportingSku() == ReportingSku &&
            CTACs[i].GetReportingVersion() == ReportingVersion &&
            CTACs[i].GetMachineType() == MachineType &&
            CTACs[i].GetFlightRing() == FlightRing &&
            CTACs[i].GetFlightingBranchName() == FlightingBranchName &&
            CTACs[i].GetBranchReadinessLevel() == BranchReadinessLevel &&
            CTACs[i].GetReleaseType() == ReleaseType &&
            CTACs[i].GetCurrentBranch() == CurrentBranch &&
            CTACs[i].SyncCurrentVersionOnly == SyncCurrentVersionOnly
        ) {
            return;
        }
    }
    CTACs.Add(new(ReportingSku, ReportingVersion, MachineType, FlightRing, FlightingBranchName, BranchReadinessLevel, CurrentBranch, ReleaseType, SyncCurrentVersionOnly));
}

static class CTACExtension
{
    public static OSSkuId GetReportingSku(this CTAC ctac) => (OSSkuId)Int32.Parse(Regex.Match(ctac.DeviceAttributes, @"(?>(?>^E:)|&)OSSkuId=(?'sku'\d+)(?>&|$)").Groups["sku"].Value);
    public static String GetReportingVersion(this CTAC ctac) => Regex.Match(ctac.Products, @"(?>^|&)V=(?'version'(?>[^&;]+))(?>&|;$)").Groups["version"].Value;
    public static MachineType GetMachineType(this CTAC ctac) => (MachineType)Enum.Parse(typeof(MachineType), Regex.Match(ctac.Products, @"(?>^|&)PN=[^&]*\.(?'arch'(?>[^&\.;]+))(?>&|;$)").Groups["arch"].Value);
    public static String GetFlightRing(this CTAC ctac) => Regex.Match(ctac.DeviceAttributes, @"(?>(?>^E:)|&)FlightRing=(?'ring'[^&]+)(?>&|$)").Groups["ring"].Value;
    public static String GetFlightingBranchName(this CTAC ctac) => Regex.Match(ctac.DeviceAttributes, @"(?>(?>^E:)|&)FlightingBranchName=(?'branch'[^&]*)(?>&|$)").Groups["branch"].Value;
    public static String GetBranchReadinessLevel(this CTAC ctac) => Regex.Match(ctac.DeviceAttributes, @"(?>(?>^E:)|&)BranchReadinessLevel=(?'readiness'[^&]+)(?>&|$)").Groups["readiness"].Value;
    public static String GetReleaseType(this CTAC ctac) => Regex.Match(ctac.DeviceAttributes, @"(?>(?>^E:)|&)ReleaseType=(?'reltype'[^&]+)(?>&|$)").Groups["reltype"].Value;
    public static String GetCurrentBranch(this CTAC ctac) => Regex.Match(ctac.Products, @"(?>^|&)Branch=(?'branch'(?>[^&;]+))(?>&|;$)").Groups["branch"].Value;

    public static String CTACParams(this CTAC ctac) =>
        $"{ctac.GetReportingSku()} {ctac.GetReportingVersion()} {ctac.GetMachineType()} {ctac.GetFlightRing()} {ctac.GetFlightingBranchName()} {ctac.GetBranchReadinessLevel()} {ctac.GetReleaseType()} {ctac.GetCurrentBranch()}";
}
