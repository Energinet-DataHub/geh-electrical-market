﻿// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Commands.ProcessDelegations;
using Energinet.DataHub.ElectricityMarket.Application.Handlers;
using Energinet.DataHub.ElectricityMarket.Application.Mappers;
using Energinet.DataHub.ElectricityMarket.Domain;
using Energinet.DataHub.ElectricityMarket.Domain.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models.Actors;
using Energinet.DataHub.ElectricityMarket.Domain.Models.GridAreas;
using Energinet.DataHub.ElectricityMarket.Domain.Repositories;
using Energinet.DataHub.ElectricityMarket.Integration.Models.ProcessDelegation;
using Energinet.DataHub.ElectricityMarket.UnitTests.Common;
using Moq;
using Xunit;
using Xunit.Categories;
using DelegatedProcess = Energinet.DataHub.ElectricityMarket.Domain.Models.Common.DelegatedProcess;

namespace Energinet.DataHub.ElectricityMarket.UnitTests.Handlers;

[UnitTest]
public sealed class GetProcessDelegationHandlerTests
{
    [Fact]
    public async Task Handle_NoActorsFound_ThrowsNotFoundException()
    {
        // Arrange
        var gridAreaRepository = new Mock<IGridAreaRepository>();
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();

        var actorRepository = new Mock<IActorRepository>();
        actorRepository
            .Setup(repo => repo.GetActorsByNumberAsync(It.IsAny<ActorNumber>()))
            .ReturnsAsync([]);

        var request = new ProcessDelegationRequestDto("XXX", EicFunctionMapper.Map(EicFunction.BalanceResponsibleParty), "111", DelegationProcessMapper.Map(DelegatedProcess.ReceiveEnergyResults));
        var command = new GetProcessDelegationCommand(request);

        var target = new GetProcessDelegationHandler(actorRepository.Object, gridAreaRepository.Object, processDelegationRepository.Object);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => target.Handle(command, CancellationToken.None));
        Assert.Equal($"No actors with number: {request.ActorNumber} found", ex.Message);
    }

    [Fact]
    public async Task Handle_DelegatedByActorNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var gridAreaRepository = new Mock<IGridAreaRepository>();
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();

        var actorRepository = new Mock<IActorRepository>();
        actorRepository
            .Setup(repo => repo.GetActorsByNumberAsync(It.IsAny<ActorNumber>()))
            .ReturnsAsync([TestPreparationModels.MockedActor()]);

        var request = new ProcessDelegationRequestDto("XXX", EicFunctionMapper.Map(EicFunction.BalanceResponsibleParty), "111", DelegationProcessMapper.Map(DelegatedProcess.ReceiveEnergyResults));
        var command = new GetProcessDelegationCommand(request);

        var target = new GetProcessDelegationHandler(actorRepository.Object, gridAreaRepository.Object, processDelegationRepository.Object);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => target.Handle(command, CancellationToken.None));
        Assert.Equal($"Market role: {request.ActorRole} was not found for actor: {request.ActorNumber}", ex.Message);
    }

    [Fact]
    public async Task Handle_GridAreaNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();

        var actorRepository = new Mock<IActorRepository>();
        var mockActor = TestPreparationModels.MockedActor();
        actorRepository
            .Setup(repo => repo.GetActorsByNumberAsync(It.IsAny<ActorNumber>()))
            .ReturnsAsync([mockActor]);

        var gridAreaRepository = new Mock<IGridAreaRepository>();
        gridAreaRepository
            .Setup(repo => repo.GetGridAreaAsync(new GridAreaCode(It.IsAny<string>())))
            .ReturnsAsync((GridArea?)null);

        var request = new ProcessDelegationRequestDto(mockActor.ActorNumber.Value, EicFunctionMapper.Map(mockActor.MarketRole.Function), "111", DelegationProcessMapper.Map(DelegatedProcess.ReceiveEnergyResults));
        var command = new GetProcessDelegationCommand(request);

        var target = new GetProcessDelegationHandler(actorRepository.Object, gridAreaRepository.Object, processDelegationRepository.Object);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => target.Handle(command, CancellationToken.None));
        Assert.Equal($"The grid area with code: {request.GridAreaCode} was not found", ex.Message);
    }

    [Fact]
    public async Task Handle_NoProcessDelegationFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorRepository = new Mock<IActorRepository>();
        var mockActor = TestPreparationModels.MockedActor();
        var mockGridArea = TestPreparationModels.MockedGridArea();
        actorRepository
            .Setup(repo => repo.GetActorsByNumberAsync(It.IsAny<ActorNumber>()))
            .ReturnsAsync([mockActor]);

        var gridAreaRepository = new Mock<IGridAreaRepository>();
        gridAreaRepository
            .Setup(repo => repo.GetGridAreaAsync(new GridAreaCode(mockGridArea.Code.Value)))
            .ReturnsAsync(mockGridArea);

        var processDelegationRepository = new Mock<IProcessDelegationRepository>();
        processDelegationRepository
            .Setup(repo => repo.GetProcessDelegationAsync(mockActor.Id, It.IsAny<DelegatedProcess>()))
            .ReturnsAsync((ProcessDelegation?)null);

        var request = new ProcessDelegationRequestDto(mockActor.ActorNumber.Value, EicFunctionMapper.Map(mockActor.MarketRole.Function), mockGridArea.Code.Value, DelegationProcessMapper.Map(DelegatedProcess.ReceiveEnergyResults));
        var command = new GetProcessDelegationCommand(request);

        var target = new GetProcessDelegationHandler(actorRepository.Object, gridAreaRepository.Object, processDelegationRepository.Object);

        // Act + Assert
        var response = await target.Handle(command, CancellationToken.None);
        Assert.Null(response);
    }

    [Fact]
    public async Task Handle_NoDelegatedProcessesFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorRepository = new Mock<IActorRepository>();
        var mockActor = TestPreparationModels.MockedActor();
        var mockGridArea = TestPreparationModels.MockedGridArea();
        actorRepository
            .Setup(repo => repo.GetActorsByNumberAsync(mockActor.ActorNumber))
            .ReturnsAsync([mockActor]);

        var gridAreaRepository = new Mock<IGridAreaRepository>();
        gridAreaRepository
            .Setup(repo => repo.GetGridAreaAsync(new GridAreaCode(mockGridArea.Code.Value)))
            .ReturnsAsync(mockGridArea);

        var processDelegation = new ProcessDelegation(new ProcessDelegationId(Guid.NewGuid()), mockActor.Id, DelegatedProcess.RequestEnergyResults, []);
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();
        processDelegationRepository
            .Setup(repo => repo.GetProcessDelegationAsync(mockActor.Id, processDelegation.DelegatedProcess))
            .ReturnsAsync(processDelegation);

        var request = new ProcessDelegationRequestDto(mockActor.ActorNumber.Value, EicFunctionMapper.Map(mockActor.MarketRole.Function), mockGridArea.Code.Value, DelegationProcessMapper.Map(processDelegation.DelegatedProcess));
        var command = new GetProcessDelegationCommand(request);

        var target = new GetProcessDelegationHandler(actorRepository.Object, gridAreaRepository.Object, processDelegationRepository.Object);

        // Act + Assert
        var response = await target.Handle(command, CancellationToken.None);
        Assert.Null(response);
    }

    [Fact]
    public async Task Handle_NoDelegatedPeriodsFound_ThrowsNotFoundException()
    {
        // Arrange
        var actorRepository = new Mock<IActorRepository>();
        var mockActor = TestPreparationModels.MockedActor();
        var mockDelegatedToActor = TestPreparationModels.MockedActor();
        var mockGridArea = TestPreparationModels.MockedGridArea();
        actorRepository
            .Setup(repo => repo.GetActorsByNumberAsync(mockActor.ActorNumber))
            .ReturnsAsync([mockActor]);

        var gridAreaRepository = new Mock<IGridAreaRepository>();
        gridAreaRepository
            .Setup(repo => repo.GetGridAreaAsync(new GridAreaCode(mockGridArea.Code.Value)))
            .ReturnsAsync(mockGridArea);

        var processDelegation = new ProcessDelegation(
            new ProcessDelegationId(Guid.NewGuid()),
            mockActor.Id,
            DelegatedProcess.RequestEnergyResults,
            [new DelegationPeriod(mockDelegatedToActor.Id, new GridAreaId(Guid.NewGuid()), NodaTime.Instant.MinValue, null)]);
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();
        processDelegationRepository
            .Setup(repo => repo.GetProcessDelegationAsync(mockActor.Id, processDelegation.DelegatedProcess))
            .ReturnsAsync(processDelegation);

        var request = new ProcessDelegationRequestDto(mockActor.ActorNumber.Value, EicFunctionMapper.Map(mockActor.MarketRole.Function), mockGridArea.Code.Value, DelegationProcessMapper.Map(processDelegation.DelegatedProcess));
        var command = new GetProcessDelegationCommand(request);

        var target = new GetProcessDelegationHandler(actorRepository.Object, gridAreaRepository.Object, processDelegationRepository.Object);

        // Act + Assert
        var response = await target.Handle(command, CancellationToken.None);
        Assert.Null(response);
    }

    [Fact]
    public async Task Handle_FoundDelegations_ReturnsSuccesful()
    {
        // Arrange
        var actorRepository = new Mock<IActorRepository>();
        var mockActor = TestPreparationModels.MockedActor();
        var mockDelegatedToActor = TestPreparationModels.MockedActor();
        var mockGridArea = TestPreparationModels.MockedGridArea();
        actorRepository
            .Setup(repo => repo.GetActorsByNumberAsync(mockActor.ActorNumber))
            .ReturnsAsync([mockActor]);

        actorRepository
            .Setup(repo => repo.GetAsync(mockDelegatedToActor.Id))
            .ReturnsAsync(mockDelegatedToActor);

        var gridAreaRepository = new Mock<IGridAreaRepository>();
        gridAreaRepository
            .Setup(repo => repo.GetGridAreaAsync(new GridAreaCode(mockGridArea.Code.Value)))
            .ReturnsAsync(mockGridArea);

        var processDelegation = new ProcessDelegation(
            new ProcessDelegationId(Guid.NewGuid()),
            mockActor.Id,
            DelegatedProcess.RequestEnergyResults,
            [new DelegationPeriod(mockDelegatedToActor.Id, mockGridArea.Id, NodaTime.Instant.MinValue, null)]);
        var processDelegationRepository = new Mock<IProcessDelegationRepository>();
        processDelegationRepository
            .Setup(repo => repo.GetProcessDelegationAsync(mockActor.Id, processDelegation.DelegatedProcess))
            .ReturnsAsync(processDelegation);

        var request = new ProcessDelegationRequestDto(mockActor.ActorNumber.Value, EicFunctionMapper.Map(mockActor.MarketRole.Function), mockGridArea.Code.Value, DelegationProcessMapper.Map(processDelegation.DelegatedProcess));
        var command = new GetProcessDelegationCommand(request);

        var target = new GetProcessDelegationHandler(actorRepository.Object, gridAreaRepository.Object, processDelegationRepository.Object);

        // Act + Assert
        var response = await target.Handle(command, CancellationToken.None);
        Assert.NotNull(response);
        Assert.Equal(mockDelegatedToActor.ActorNumber.Value, response.ActorNumber);
        Assert.Equal(mockDelegatedToActor.MarketRole.Function.ToString(),  response.ActorRole.ToString());
    }
}
