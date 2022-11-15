// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using UPA_EXTERNAL_MODELS;
using UPA_EXTERNAL_MODELS.Models.Configs;
using UPA_SDK;
using UPAExternalAPI.Models.Vendor;

internal class Program
{
    private static void Main(string[] args)
    {
        ///Add Provided Authentication Data to file "config.json" and this project will be functional
        LocalConfig conf = JsonConvert.DeserializeObject<LocalConfig>(File.ReadAllText("config.json"));
        ///Runtime.json will contains the latest PO offset you have downloaded
        RuntimeVariables _runtime = JsonConvert.DeserializeObject<RuntimeVariables>(File.ReadAllText("Runtime.json"));

        ///Create a new instance of Vendor SDK CLass that wraps all vendor API
        Vendor _vendorObject = new Vendor(conf.API_ROOT_URL,
                          conf.API_KEY,
                          conf.API_USER,
                          conf.API_PASSWORD,
                          conf.API_OTP_SECRET,
                          conf.API_SECRET,
                          15);
        //Login to the API server
        var reso = _vendorObject.Login();
        if (!reso)// Failure Login
        {
            Console.WriteLine("FAILED Login");
            return;
        }
        Console.WriteLine("Successfull Login");
        int localOffset = _runtime.LatestPoOffset;//Set this variable to the latest stored offset in the "Runtime.json" File
        //Initial search for PO starting from localOffset
        ResponseContainer<List<PoSearchResponse>> result = _vendorObject.PoSearch(new UPAExternalAPI.Models.Vendor.PoSearchRequest
        {
            Offset = localOffset
        });
        //Keep looping until no result is retreived the you have finished downloading all your PO
        while (result != null && result.resultLength > 0)
        {
            localOffset += result.resultLength;
            result.Result.ForEach(r =>
            {
                Console.WriteLine($"\t{r.Po}");
            });
            Console.WriteLine("Starting Details >>>>>>>>>");
            //Get details of downloaded PO
            ADD_PO_Details(result.Result, _vendorObject);
            result = _vendorObject.PoSearch(new UPAExternalAPI.Models.Vendor.PoSearchRequest
            {
                Offset = localOffset
            });
        }
        _runtime.LatestPoOffset = localOffset + 1;
        File.WriteAllText("Runtime.json", _runtime.SerializeObject());
        Console.WriteLine($" FInished ==>{localOffset}");
    }
    //This method gets the distribution list of all your Purchase Orders
    private static void ADD_PO_Details(List<PoSearchResponse> result, Vendor vendor)
    {
        if (result == null)
            return;
        foreach (var po in result)
        {
            var detItems = vendor.PoDistributeListDetailedItems(new PoDistributionDetailsQuery
            {
                po = po.Po.Value
            });
            if (detItems == null || detItems.Result == null)
            {
                Console.WriteLine($"Null DEtails >> {po.Po}");
                continue;
            }
            //detItems contains list of all distribution list of this PO
            detItems.Result.ForEach(po_row =>
            {
                Console.WriteLine($"\t PO {po_row.Po} , PR {po_row.Pr} ,PR_DET = {po_row.PrDetId} , PO_DET ={po_row.PoDetId}  DETAIL Added ");

            });
        }
    }
}