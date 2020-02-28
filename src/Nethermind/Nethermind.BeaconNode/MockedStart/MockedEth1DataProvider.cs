﻿//  Copyright (c) 2018 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nethermind.BeaconNode.Services;
using Nethermind.Core2;
using Nethermind.Core2.Configuration;
using Nethermind.Core2.Containers;
using Nethermind.Core2.Crypto;
using Nethermind.Core2.Types;

namespace Nethermind.BeaconNode.MockedStart
{
    public class MockedEth1DataProvider : IEth1DataProvider
    {
        private readonly IOptionsMonitor<TimeParameters> _timeParameterOptions;
        private readonly IOptionsMonitor<HonestValidatorConstants> _honestValidatorConstantOptions;
        private readonly ICryptographyService _cryptographyService;
        private readonly BeaconStateAccessor _beaconStateAccessor;
        private readonly ForkChoice _forkChoice;
        private readonly IStoreProvider _storeProvider;

        public MockedEth1DataProvider(IOptionsMonitor<TimeParameters> timeParameterOptions,
            IOptionsMonitor<HonestValidatorConstants> honestValidatorConstantOptions,
            ICryptographyService cryptographyService,
            BeaconStateAccessor beaconStateAccessor,
            ForkChoice forkChoice, 
            IStoreProvider storeProvider)
        {
            _timeParameterOptions = timeParameterOptions;
            _honestValidatorConstantOptions = honestValidatorConstantOptions;
            _cryptographyService = cryptographyService;
            _beaconStateAccessor = beaconStateAccessor;
            _forkChoice = forkChoice;
            _storeProvider = storeProvider;
        }
        
        public Task<ulong> GetDistanceAsync(Bytes32 eth1BlockHash)
        {
            ulong distance = 2 * _honestValidatorConstantOptions.CurrentValue.Eth1FollowDistance;
            return Task.FromResult(distance);
        }

        public async Task<Eth1Data> GetEth1DataAsync(ulong distance)
        {
            if (!_storeProvider.TryGetStore(out IStore? store))
            {
                throw new Exception("Store not available.");
            }

            Root head = await _forkChoice.GetHeadAsync(store!);
            BeaconState state = await store!.GetBlockStateAsync(head);
            Epoch currentEpoch = _beaconStateAccessor.GetCurrentEpoch(state!);
            Eth1Data eth1Data = GetEth1DataStub(state!, currentEpoch);
            
            return eth1Data;
        }

        public IAsyncEnumerable<Deposit> GetDepositsAsync(Bytes32 eth1BlockHash, ulong startIndex, ulong maximum)
        {
            // Mocked data returns no extra deposits, but because count doesn't increase, then it won't actually be called. If it is called, there is a problem, so throw.
            throw new NotImplementedException();
        }

        public Eth1Data GetEth1DataStub(BeaconState state, Epoch currentEpoch)
        {
            TimeParameters timeParameters = _timeParameterOptions.CurrentValue;
            
            uint epochsPerPeriod = timeParameters.SlotsPerEth1VotingPeriod / timeParameters.SlotsPerEpoch;
            ulong votingPeriod = (ulong)currentEpoch / epochsPerPeriod;
            Span<byte> votingPeriodBytes = stackalloc byte[32];
            BinaryPrimitives.WriteUInt64LittleEndian(votingPeriodBytes, votingPeriod);
            Bytes32 hashOfVotingPeriod = _cryptographyService.Hash(votingPeriodBytes);
            
            Root depositRoot = new Root(hashOfVotingPeriod.AsSpan());
            ulong depositCount = state.Eth1DepositIndex;
            Bytes32 blockHash = _cryptographyService.Hash(hashOfVotingPeriod.AsSpan());
            Eth1Data eth1Data = new Eth1Data(depositRoot, depositCount, blockHash);
            
            return eth1Data;
        }
    }
}