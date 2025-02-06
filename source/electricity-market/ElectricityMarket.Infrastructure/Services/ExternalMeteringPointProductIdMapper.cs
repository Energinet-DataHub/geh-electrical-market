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

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Services;

public static class ExternalMeteringPointProductIdMapper
{
    public static string Map(string externalValue)
    {
        return externalValue switch
        {
            "5790001330590" => "Tariff",
            "5790001330606" => "FuelQuantity",
            "8716867000016" => "PowerActive",
            "8716867000023" => "PowerReactive",
            "8716867000030" => "EnergyActive",
            "8716867000047" => "EnergyReactive",
            _ => $"Unmapped: {externalValue}",
        };
    }
}
