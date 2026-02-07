using AFKJourneyBot.Common;
using AFKJourneyBot.Core.Runtime;
using AFKJourneyBot.Core.Tasks.Shared;
using Serilog;

namespace AFKJourneyBot.Core.Tasks;

public class HomesteadOrders(IBotApi botApi) : IBotTask
{
    private const bool CraftMultiplier5 = true; // TODO: Make configurable
    private static readonly ProductionBuilding[] ProductionBuildingTemplates =
    [
        new("Kitchen", "homestead/kitchen.png", "homestead/enter_kitchen.png"),
        new("Forge", "homestead/forge.png", "homestead/enter_forge.png"),
        new("Alchemy Workshop", "homestead/alchemy_workshop.png", "homestead/enter_alchemy_workshop.png")
    ];

    public const string TaskName = "Homestead Orders";
    public string Name => TaskName;

    public async Task RunAsync(CancellationToken ct)
    {
        await NavigationUtils.EnsureMainViewAsync(botApi, ct);

        Log.Information("Entering Homestead");
        await botApi.TapAsync(new ScreenPoint(1010, 1620), ct);


        var outOfStamina = false;
        var itemsCrafted = 0;
        var requestsDelivered = 0;
        while (!ct.IsCancellationRequested && !outOfStamina){
            // Loop through production buildings
            foreach (var productionBuilding in ProductionBuildingTemplates)
            {
                Log.Information("Navigating to {Building}", productionBuilding.Name);
                await WaitAndTap("homestead/overview.png", botApi, ct);
                await WaitAndTap("homestead/buildings.png", botApi, ct);
                await WaitAndTap("homestead/production.png", botApi, ct);
                await WaitAndTap(productionBuilding.OverviewButtonTemplate, botApi, ct);
                await WaitAndTap("homestead/go.png", botApi, ct);
                await WaitAndTap(productionBuilding.EnterBuildingTemplate, botApi, ct);
                
                while (!outOfStamina) // Within one production building, loop until there are no more 'Requested' labels
                {
                    await Task.Delay(2000, ct);
                    var cardUpgrade = await botApi.FindTemplateAsync("homestead/card_upgrade_yes.png", ct);
                    if (cardUpgrade is not null)
                    {
                        Log.Information("Upgrading a Process Card");
                        await botApi.TapAsync(cardUpgrade.Value, ct);
                        await WaitAndTap("homestead/upgrade.png", botApi, ct);
                        await Task.Delay(3000, ct);
                        await botApi.BackAsync(ct);
                        await Task.Delay(500, ct);
                        await botApi.BackAsync(ct);
                    }
                    var itemPoint = await botApi.FindTemplateAsync("homestead/requested_item.png", ct);
                    if (itemPoint is null)
                    {
                        Log.Information("No requested items found in {Building}", productionBuilding.Name);
                        await botApi.BackAsync(ct);
                        break;
                    }
                    await botApi.TapAsync(itemPoint.Value, ct);
                    await Task.Delay(500, ct);
                    
                    if (CraftMultiplier5)
                    {
                        while (await botApi.FindTemplateAsync("homestead/x5.png", ct) is null)
                        {
                            await botApi.TapAsync(new ScreenPoint(785, 1690), ct);
                            await Task.Delay(500, ct);
                        }
                    }

                    if ((await botApi.GetPixelAsync(new ScreenPoint(335, 1725), ct)).G < 175)
                    {
                        // Enable auto
                        await botApi.TapAsync(new ScreenPoint(335, 1725), ct);
                    }
                    while (!outOfStamina) // Until we get the "Go to requests" popup, craft the same item on loop
                    {
                        if (await botApi.GetPixelAsync(new ScreenPoint(623, 1737), ct) == new RgbColor(107, 107, 107))
                        {
                            Log.Information("Out of stamina");
                            outOfStamina = true;
                            break;
                        }
                        await botApi.TapAsync(new ScreenPoint(540, 1715), ct);
                        await Task.Delay(1500, ct);
                        if (await botApi.FindTemplateAsync("homestead/go_to_requests.png", ct) is not null)
                        {
                            Log.Information("Moving on to the next item");
                            await botApi.BackAsync(ct);
                            await Task.Delay(500, ct);
                            await botApi.BackAsync(ct);
                            break;
                        }
                        await WaitForCraftingToFinish(botApi, ct);
                        Log.Information("Crafted item");
                        itemsCrafted += CraftMultiplier5 ? 5 : 1;
                    }
                }
                
                if (outOfStamina)
                {
                    await botApi.BackAsync(ct);
                    await Task.Delay(1000, ct);
                    await botApi.BackAsync(ct);
                    break;
                }
            }
        
            //Deliver the crafted items
            Log.Information("Delivering Requests");
            await WaitAndTap("homestead/requests.png", botApi, ct);
            while(true)
            {
                var deliverableRequest = await botApi.WaitForTemplateAsync("homestead/deliverable_request.png", ct,
                    timeout: TimeSpan.FromSeconds(5), threshold:0.92d, errorOnFail: false);
                if (deliverableRequest is null)
                {
                    Log.Information("No more deliverable requests");
                    break;
                }
                await botApi.TapAsync(deliverableRequest.Value.Add(-50, 70), ct);
                await WaitAndTap("homestead/quick_select.png", botApi, ct);
                await WaitAndTap("homestead/deliver.png", botApi, ct);
                await Task.Delay(1000, ct);
                await botApi.TapAsync(new ScreenPoint(1000, 1820), ct); // "Tap to close"
                await Task.Delay(1000, ct);
                Log.Information("Delivered a request");
                requestsDelivered++;
            }

            if (outOfStamina)
            {
                Log.Information("Out of stamina and deliveries, stopping task");
                break;
            }

            await botApi.BackAsync(ct);
        }

        Log.Information("Crafted {ItemsCrafted} items and delivered {RequestsDelivered} requests", itemsCrafted,
            requestsDelivered);
    }

    private static async Task WaitAndTap(string template, IBotApi botApi, CancellationToken ct)
    {
        var point = await botApi.WaitForTemplateAsync(template, ct);
        await botApi.TapAsync(point!.Value, ct);
    }

    private static async Task WaitForCraftingToFinish(IBotApi botApi, CancellationToken ct)
    {
        // Wait for crafting to start (big button in the lower middle becomes white)
        while (await botApi.GetPixelAsync(new ScreenPoint(600, 1670), ct) != new RgbColor(249, 245, 238))
        {
            await Task.Delay(500, ct);
        }
        
        // Wait for crafting to end (big button stops being white)
        while (await botApi.GetPixelAsync(new ScreenPoint(600, 1670), ct) == new RgbColor(249, 245, 238))
        {
            await Task.Delay(500, ct);
        }
    }

    private record ProductionBuilding(string Name, string OverviewButtonTemplate, string EnterBuildingTemplate);
}
