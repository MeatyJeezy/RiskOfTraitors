using RoR2;
using UnityEngine;

namespace RiskOfTraitors
{
    public class AverageItems : MonoBehaviour
    {
        //Inventory collective = new();
        //int[] allRedItemIndexes = {2, 4, 11, };//{2, 5, 14, 15, 22, 23, 31, 32, 35, 38, 50, 51, 66, 68, 69, 70, 71, 75, 80, 85, 95};
        //int[] allGreenItemIndexes = {3, 4, 9, 11, 13, 19, 21, 25, 26, 30, 33, 37, 46, 62, 63, 64, 65, 76, 78, 79, 86, 90, 94};
        //int[] allWhiteItemIndexes = {0, 1, 6, 7, 8, 16, 17, 20, 24, 27, 29, 36, 39, 41, 57, 58, 59, 60, 61, 84, 87, 91};
        //public static int itemsBelowAverage = 1;
        //public static int itemsAboveAverage = 1;
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
        // "averages" items by removing random items from the rich and giving random items to the poor.
        public void RedistributeItems()
        {
            Log.Info("Started RedistributeItems");
            int playerCount = PlayerCharacterMasterController._instancesReadOnly.Count;
            int[] playerItemCount = new int[playerCount];
            int totalItemCount = 0;
            int averageItemCount = 0;
            int itemsBelowAverage = RiskOfTraitors.itemsBelowAverage;
            int itemsAboveAverage = RiskOfTraitors.itemsAboveAverage;
            bool enableItemAveraging = RiskOfTraitors.enableItemAveraging;
            bool removeLunarItems = RiskOfTraitors.removeLunarItems;
            Log.Info($"Player Count is: {playerCount}");
            for (int i = 0; i < playerCount; i++)
            {
                if (PlayerCharacterMasterController._instancesReadOnly[i].master != null)
                {
                    playerItemCount[i] += PlayerCharacterMasterController._instancesReadOnly[i].master.inventory.GetTotalItemCountOfTier(ItemTier.Tier1);
                    playerItemCount[i] += PlayerCharacterMasterController._instancesReadOnly[i].master.inventory.GetTotalItemCountOfTier(ItemTier.Tier2);
                    playerItemCount[i] += PlayerCharacterMasterController._instancesReadOnly[i].master.inventory.GetTotalItemCountOfTier(ItemTier.Tier3);
                    playerItemCount[i] += PlayerCharacterMasterController._instancesReadOnly[i].master.inventory.GetTotalItemCountOfTier(ItemTier.Lunar);
                    playerItemCount[i] += PlayerCharacterMasterController._instancesReadOnly[i].master.inventory.GetTotalItemCountOfTier(ItemTier.Boss);
                    playerItemCount[i] += PlayerCharacterMasterController._instancesReadOnly[i].master.inventory.GetTotalItemCountOfTier(ItemTier.VoidTier1);
                    playerItemCount[i] += PlayerCharacterMasterController._instancesReadOnly[i].master.inventory.GetTotalItemCountOfTier(ItemTier.VoidTier2);
                    playerItemCount[i] += PlayerCharacterMasterController._instancesReadOnly[i].master.inventory.GetTotalItemCountOfTier(ItemTier.VoidTier3);
                    playerItemCount[i] += PlayerCharacterMasterController._instancesReadOnly[i].master.inventory.GetTotalItemCountOfTier(ItemTier.VoidBoss);
                    totalItemCount += playerItemCount[i];

                    Log.Info($"playerItemCount {i}: {playerItemCount[i]}");
                }
                else
                {
                    Log.Warning($"Player body at index {i} was null");
                }
            }
            averageItemCount = totalItemCount / playerCount;
            Log.Info($"totalItemCount: {totalItemCount}");
            Log.Info($"averageItemCount: {averageItemCount}");
            for (int i = 0; i < playerCount;i++)
            {
                // If Lunar item removal is active
                int lunarItemCount = PlayerCharacterMasterController._instancesReadOnly[i].master.inventory.GetTotalItemCountOfTier(ItemTier.Lunar);
                if (lunarItemCount > 0 && removeLunarItems)
                {
                    int j = 0;
                    ItemIndex[] indexesToRemove = new ItemIndex[lunarItemCount];
                    foreach (var itemindex in PlayerCharacterMasterController.instances[i].master.inventory.itemAcquisitionOrder)
                    //for (int j = 0; j < PlayerCharacterMasterController.instances[i].master.inventory.itemAcquisitionOrder.Count; j++)
                    {
                        //var itemindex = PlayerCharacterMasterController.instances[i].master.inventory.itemAcquisitionOrder[j];
                        var item = ItemCatalog.GetItemDef(itemindex);
                        if (item.tier == ItemTier.Lunar)
                        {
                            indexesToRemove[j] = itemindex;
                            j++;
                            Log.Info($"Lunar item index {itemindex} found in inventory of {PlayerCharacterMasterController.instances[i].GetDisplayName()}");
                        }
                    }
                    for (int k = 0; k < lunarItemCount; k++)
                    {
                        PlayerCharacterMasterController.instances[i].master.inventory.RemoveItem(indexesToRemove[k]);
                        Log.Info($"Successfully removed lunar item index: {indexesToRemove[k]}");
                    }
                }
                // Give an item if they're below the average item count by 1 or more
                while (playerItemCount[i] < averageItemCount - itemsBelowAverage && enableItemAveraging)
                {
                    Log.Info($"Giving item to player at index {i}");
                    //GivePlayerAtIndexRandomItem(i);
                    PlayerCharacterMasterController.instances[i].master.inventory.GiveRandomItems(1, false, false);
                    playerItemCount[i]++;
                    Log.Info($"Player {i} new item count: {playerItemCount[i]}");

                }
                // Remove an item if they're above the average item count (+1), give if they're below. + 1 to averageItemCount to give a very slight advantage for picking up more items.
                while (playerItemCount[i] > averageItemCount + itemsAboveAverage && enableItemAveraging)
                {
                    int itemIndexToRemove = UnityEngine.Random.Range(0, PlayerCharacterMasterController.instances[i].master.inventory.itemAcquisitionOrder.Count);
                    Log.Info($"Remove item index {itemIndexToRemove} from player {PlayerCharacterMasterController.instances[i].GetDisplayName()}");
                    // This picks a random ItemIndex from the itemAcquisitionOrder list and subtracts one from it. Seems to work fine?
                    PlayerCharacterMasterController.instances[i].master.inventory.RemoveItem(PlayerCharacterMasterController.instances[i].master.inventory.itemAcquisitionOrder[itemIndexToRemove], 1);
                    playerItemCount[i]--;
                }
                
            }
        }
        /*public void GivePlayerAtIndexRandomItem(int index) {
            var randomInt = UnityEngine.Random.Range(0, 100);
            switch (randomInt)
            {
                // 0-84: White
                default:
                    int randomWhiteItemIndex = UnityEngine.Random.Range(0, allWhiteItemIndexes.Length);
                    PlayerCharacterMasterController.instances[index].master.inventory.GiveItem((ItemIndex)allWhiteItemIndexes[randomWhiteItemIndex], 1);
                    Log.Info($"Given White Item index: {randomWhiteItemIndex} to player {PlayerCharacterMasterController.instances[index].GetDisplayName()}");
                    break;

                // 85-96: Green
                case 85:
                case 86:
                case 87:
                case 88:
                case 89:
                case 90:
                case 91:
                case 92:
                case 93:
                case 94:
                case 95:
                case 96:
                    int randomGreenItemIndex = UnityEngine.Random.Range(0, allGreenItemIndexes.Length);
                    PlayerCharacterMasterController.instances[index].master.inventory.GiveItem((ItemIndex)allGreenItemIndexes[randomGreenItemIndex], 1);
                    Log.Info($"Given Green Item index: {randomGreenItemIndex} to player {PlayerCharacterMasterController.instances[index].GetDisplayName()}");
                    break;

                // Red
                case 97:
                case 98:
                case 99:
                    int randomRedItemIndex = UnityEngine.Random.Range(0, allRedItemIndexes.Length);
                    PlayerCharacterMasterController.instances[index].master.inventory.GiveItem((ItemIndex)allRedItemIndexes[randomRedItemIndex], 1);
                    Log.Info($"Given Red Item index: {randomRedItemIndex} to player {PlayerCharacterMasterController.instances[index].GetDisplayName()}");
                    break;
            }
        }*/
    }
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
}
