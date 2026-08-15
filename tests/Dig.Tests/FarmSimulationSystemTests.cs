using System;
using Dig.Application.Farming;
using Dig.Application.Runtime;
using Dig.Domain.Core;
using Dig.Domain.Farming;
using Dig.Domain.Runtime;
using Xunit;

namespace Dig.Tests
{

public sealed class FarmSimulationSystemTests
{
    [Fact]
    public void Execute_advances_registered_farms_with_authoritative_tick()
    {
        InMemoryFarmRepository repository = new InMemoryFarmRepository();
        EntityId farmId = EntityId.New();
        FarmState farm = new FarmState(FarmMode.Hamsters);
        farm.Deliver(FarmDeliveryKind.Hamster, FarmOperationPolicy.HamsterBreederReserve, 0);
        farm.Deliver(FarmDeliveryKind.MushroomFeed, FarmOperationPolicy.FeedCapacity, 0);
        repository.Save(farmId, farm);

        FarmSimulationSystem system = new FarmSimulationSystem(repository);
        SimulationState simulation = SimulationState.Create(
            123UL,
            TimeSpan.FromSeconds(1));

        system.Execute(new SimulationContext(
            FarmOperationPolicy.HamsterReproductionTicks,
            simulation));

        FarmState? advanced = repository.Get(farmId);
        Assert.NotNull(advanced);
        Assert.Equal(3, advanced!.HamsterCount);
    }

    [Fact]
    public void Execute_progresses_animals_that_are_leaving_after_mode_switch()
    {
        InMemoryFarmRepository repository = new InMemoryFarmRepository();
        EntityId farmId = EntityId.New();
        FarmState farm = new FarmState(FarmMode.Grubs);
        farm.Deliver(FarmDeliveryKind.Grub, 3, 0);
        farm.SwitchMode(FarmMode.Mushrooms, 10);
        repository.Save(farmId, farm);

        FarmSimulationSystem system = new FarmSimulationSystem(repository);
        SimulationState simulation = SimulationState.Create(
            123UL,
            TimeSpan.FromSeconds(1));

        system.Execute(new SimulationContext(11, simulation));

        FarmState? advanced = repository.Get(farmId);
        Assert.NotNull(advanced);
        Assert.Equal(2, advanced!.EscapingGrubCount);
    }

    [Fact]
    public void Farm_system_is_scheduled_every_simulation_tick()
    {
        FarmSimulationSystem system = new FarmSimulationSystem(new InMemoryFarmRepository());

        Assert.Equal("farm-ecology", system.Name);
        Assert.Equal(1, system.IntervalTicks);
    }
}

}
