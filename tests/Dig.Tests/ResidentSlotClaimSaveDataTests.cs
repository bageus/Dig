using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Dig.Application.Saving;
using Xunit;

namespace Dig.Tests
{

public sealed class ResidentSlotClaimSaveDataTests
{
    [Fact]
    public void Data_contract_round_trip_preserves_resident_slot_claim_fields()
    {
        InventorySaveData source = new InventorySaveData
        {
            Version = 17,
        };
        source.ResidentSlotClaims.Add(new ResidentSlotClaimSaveData
        {
            JobId = "10000000000000000000000000000001",
            ResidentId = "20000000000000000000000000000001",
            ItemId = "ore.iron",
            Compartment = 1,
            SlotIndex = 3,
            Quantity = 27,
        });
        DataContractJsonSerializer serializer =
            new DataContractJsonSerializer(typeof(InventorySaveData));

        InventorySaveData restored;
        using (MemoryStream stream = new MemoryStream())
        {
            serializer.WriteObject(stream, source);
            string json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Contains("ResidentSlotClaims", json);
            Assert.Contains("SlotIndex", json);
            stream.Position = 0;
            restored = (InventorySaveData)serializer.ReadObject(stream)!;
        }

        ResidentSlotClaimSaveData claim = Assert.Single(restored.ResidentSlotClaims);
        Assert.Equal(source.Version, restored.Version);
        Assert.Equal(source.ResidentSlotClaims[0].JobId, claim.JobId);
        Assert.Equal(source.ResidentSlotClaims[0].ResidentId, claim.ResidentId);
        Assert.Equal(source.ResidentSlotClaims[0].ItemId, claim.ItemId);
        Assert.Equal(source.ResidentSlotClaims[0].Compartment, claim.Compartment);
        Assert.Equal(source.ResidentSlotClaims[0].SlotIndex, claim.SlotIndex);
        Assert.Equal(source.ResidentSlotClaims[0].Quantity, claim.Quantity);
    }
}

}