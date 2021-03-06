# Nethermind Ethereum 2.0 - Beacon Node Host

This is the Beacon Node host for Ethereum 2.0 for the .NET Core Nethermind project.

Note that the Eth 2.0 impelmentation is only an alpha version and still in initial development.

## Getting started

### Pre-requisites

* .NET Core 3.0 development tools
* On Linux and OSX, you also need GMP installed (big number library)

Enable trust of the ASP.NET Core development certificate (you can ignore in a browser, but the validator client will reject the connection). On Windows or Max, dotnet will take care of this; on Linux you will need to follow distribution specific instructions (see .NET Core guidance).

```
dotnet dev-certs https --trust
```

### Compile and run the Beacon Node

To run the unit tests:

```
dotnet test src/Nethermind/Nethermind.BeaconNode.Test
```

To run the beacon node with default Development settings (minimal config), with mocked quick start:

```
dotnet run --project src/Nethermind/Nethermind.BeaconNode.Host -- --QuickStart:GenesisTime 1578009600 --QuickStart:ValidatorCount 64
```

### Test it works

**NOTE: OpenAPI not currently fixed after upgrade to 0.10.1, so this section is not working (yet)** 

Run, as above, then open a browser to ```https://localhost:8230/node/version``` and it should respond with the name and version.

Other GET queries:

* genesis time: ```https://localhost:8230/node/genesis_time```
* fork: ```https://localhost:8230/node/fork```
* validator duties: ```https://localhost:8230/validator/duties?validator_pubkeys=0xa1c76af1545d7901214bb6be06be5d9e458f8e989c19373a920f0018327c83982f6a2ac138260b8def732cb366411ddc&validator_pubkeys=0x94f0c8535601596eb2165adb28ebe495891a3e4ea77ef501e7790cccb281827d377a5a8d4c200e3595d3f38f8633b480&validator_pubkeys=0x81283b7a20e1ca460ebd9bbd77005d557370cabb1f9a44f530c4c4c66230f675f8df8b4c2818851aa7d77a80ca5a4a5e&epoch=0```
* get an unsigned block: ```https://localhost:8230/validator/block?slot=1&randao_reveal=0xa3426b6391a29c88f2280428d5fdae9e20f4c75a8d38d0714e3aa5b9e55594dbd555c4bc685191e83d39158c3be9744d06adc34b21d2885998a206e3b3fd435eab424cf1c01b8fd562deb411348a601e83d7332d8774d1fd3bf8b88d7a33c67c```

Note: With QuickStart validator count 64, validators index 20, with public key 0xa1c76af1..., is the validator for slot 1. The corresponding randao signature for fork 0x00000000, at epoch 0, that must be used is 0xa3426b63... (other values will fail validation).

### Test with an in-process Honest Validator

This will run both the beacon node and an in-process honest validator, both using quick start mocked keys. For testing and devleopment it is easiest to run both components in a single host:

```
dotnet run --project src/Nethermind/Nethermind.BeaconNode.Host -- --QuickStart:GenesisTime 1578009600 --QuickStart:ValidatorCount 64 --QuickStart:ValidatorStartIndex 0 --QuickStart:NumberOfValidators 32
```

### Test with multiple nodes

Running the node will create an ENR record in the configured data directory. You need to get the value for the first node and pass it to the second (the example below uses PowerShell).

Ensure the host is build:

```
dotnet build src/Nethermind/Nethermind.BeaconNode.Host
``` 

In the second terminal, get the ENR record details:

```
$enr = Get-Content 'src/Nethermind/Nethermind.BeaconNode.Host/bin/Debug/netcoreapp3.1/Development/mothra/network/enr.dat'
```

Then, at the same time start the first node in one terminal (see below for clock synchronisation details):

```
$offset = [Math]::Floor((1578009600 - [DateTimeOffset]::UtcNow.ToUnixTimeSeconds())/60) * 60; $offset; dotnet run --no-build --project src/Nethermind/Nethermind.BeaconNode.Host --QuickStart:GenesisTime 1578009600 --QuickStart:ValidatorCount 64 --QuickStart:ClockOffset $offset --QuickStart:ValidatorStartIndex 32 --QuickStart:NumberOfValidators 32
```

And the second node in a second PowerShell terminal:

```
$offset = [Math]::Floor((1578009600 - [DateTimeOffset]::UtcNow.ToUnixTimeSeconds())/60) * 60; $offset; dotnet run --no-build --project src/Nethermind/Nethermind.BeaconNode.Host -- --DataDirectory Development9001 --Peering:Mothra:BootNodes:0 $enr --QuickStart:GenesisTime 1578009600 --QuickStart:ValidatorCount 64 --QuickStart:ClockOffset $offset --QuickStart:ValidatorStartIndex 0 --QuickStart:NumberOfValidators 4
```

#### Test synchronisation (second node starting after the first)

The clocks still need to be synchronised, so start the first node as above, and capture the correspond offset in the second terminal:

```
$offset = [Math]::Floor((1578009600 - [DateTimeOffset]::UtcNow.ToUnixTimeSeconds())/60) * 60; $offset;
```

The node can then be started later, but still with the same clock offset:

```
dotnet run --no-build --project src/Nethermind/Nethermind.BeaconNode.Host -- --DataDirectory Development9001 --Peering:Mothra:BootNodes:0 $enr --QuickStart:GenesisTime 1578009600 --QuickStart:ValidatorCount 64 --QuickStart:ClockOffset $offset --QuickStart:ValidatorStartIndex 0 --QuickStart:NumberOfValidators 4
```


### Test with separate processes for node and validator

**NOTE: REST API is currently broken (from spec updated to 0.10.1), so can't run in separate processes**

This will run the Beacon Node and Honest Validator in separate host processes, communicating via the REST API.

When using the quick start clock (to mock a specific genesis time), you need to synchronise the clock offset of the node and validator. The following will set genesis to occur at the next full minute. 

First, build both the required hosts:

```
dotnet build src/Nethermind/Nethermind.BeaconNode.Host
dotnet build src/Nethermind/Nethermind.HonestValidator.Host
```

Then run the node in one shell (using PowerShell):

```
$offset = [Math]::Floor((1578009600 - [DateTimeOffset]::UtcNow.ToUnixTimeSeconds())/60) * 60; $offset; dotnet run --no-build --project src/Nethermind/Nethermind.BeaconNode.Host --QuickStart:GenesisTime 1578009600 --QuickStart:ValidatorCount 64 --QuickStart:ClockOffset $offset
```

And the validator host in a separate shell (which connects to the node):

```
$offset = [Math]::Floor((1578009600 - [DateTimeOffset]::UtcNow.ToUnixTimeSeconds())/60) * 60; $offset; dotnet run --no-build --project src/Nethermind/Nethermind.HonestValidator.Host --QuickStart:ValidatorStartIndex 32 --QuickStart:NumberOfValidators 32 --QuickStart:ClockOffset $offset
```

Note that it is possible to run ranges of validators both in-process and as separate validator processes, or even have multiple validator ranges in multiple validator processes.

### Quick start clock

For a network to interact it is important that their clocks are synchronised (the specifications allow for limited tolerance). Normally this is provided by the system clock, synchronised to real time.

To test genesis, the genesis time parameter could then be adjusted to just ahead of the current system clock time, however every time the system was tested this would be different (with a different genesis block).

Whilst we could use a different genesis block each time, Nethermind also has a mock clock that can run at a specific offset, but at realtime speed. 

For example, with genesis time 1,578,009,600 this clock can be set to start at a test time slightly before 1,578,009,600 every time it is run. (The snippets above calculate the offset to use to start at the next minute boundary, so will work if node and validator are started during the same minute.)

Note that the different processess (nodes and validators) need to use the same offset to be synchronised. 

The clock will also be useful in future testing to reproduce specific scenarios starting from particular states at past clock dates (using a negative offset).

#### Example: Slot catch up when a test net is missing time

The default quickstart clock offset starts the effective clock at genesis time. You can run the host with a past genesis time and offset = 0 (current time clock) to simulate what might happen if a test net is missing some time.

e.g. To simulate the first test validator starting 120 seconds after genesis:

```
dotnet run --project src/Nethermind/Nethermind.BeaconNode.Host --QuickStart:GenesisTime ([DateTimeOffset]::UtcNow.ToUnixTimeSeconds() - 120) --QuickStart:ClockOffset 0 --QuickStart:ValidatorCount 64 --QuickStart:ValidatorStartIndex 0 --QuickStart:NumberOfValidators 32
```

Normally other nodes would have produced blocks and the chain head will match the clock time; if it is behind the clock time, it would simply mean that the node needs to sync to catch up.

However, with a test network there might have been no nodes/validators active, e.g. if the entire network was stopped. This means no blocks would have been produced (by anyone) and the head will be old.

If nodes only checked based on clock time, and the head was too many epochs in the past, then every node would simply get 406 DutiesNotAvailableForRequestedEpoch errors.

e.g. If a test network was stopped at slot 100 (epoch 12), and restarted at clock time slot 150 (epoch 18) then if validators used the clock time to start requesting duties and they all requested for epoch 18, they would all get 406 errors.

The Nethermind validator client will start at whatever head the beacon node has, i.e. slot 101, even if it is in the past (compared to the clock).

If it was only one node that was stopped, i.e. it is simply out of date and needs to sync, then it might produce some useless blocks (but there is no penalty to this), and once the beacon node has updated the head, the validator will know which duties to check next.


### Optional requirements

* PowerShell Core, to run build scripts
* An editor, e.g. VS Code, if you want to contribute

### Build with version number

To publish a release version, configured for Production environment:

```
./src/Nethermind/Nethermind.BeaconNode.Host/build.ps1
```

Then run the DLL that was published:

```
dotnet ./src/Nethermind/Nethermind.BeaconNode.Host/release/latest/Nethermind.BeaconNode.Host.dll
```

The published host is configured to use the data directory '{LocalApplicationData}/Nethermind/BeaconHost/Production'.

On Linux & OSX this is '/home/<user>/.local/share/Nethermind/BeaconHost/Production', and on Windows it is 'C:\Users\<user>\AppData\Local\Nethermind\BeaconHost\Production'

From the published version you can also start with relative data directory 'Development', which contains the minimal configuration, and quick start parameters:

```
dotnet ./src/Nethermind/Nethermind.BeaconNode.Host/release/latest/Nethermind.BeaconNode.Host.dll --DataDirectory Development --QuickStart:GenesisTime 1578009600 --QuickStart:ValidatorCount 64
```

## Development

### Configuration files

Primary configuration is based on a separate data directory for each instance, using the --DataDirectory parameter.

The main data directory is configured in the hostsettings.json file; for development this is set to 'Development', a relative directory.
 
The published version has '{LocalApplicationData}/Nethermind/BeaconHost/Production', which is resolved to a subfolder within a standard system folder.

Supported special directory tokens are:

* {CommonApplicationData}: '/usr/share' on Linux/OSX, 'C:\ProgramData' on Windows.
* {LocalApplicationData}: '/home/<user>/.local/share' on Linux/OSX, 'C:\Users\<user>\AppData\Local' on Windows.
* {ApplicationData}: '/home/<user>/.config' on Linux/OSX, 'C:\Users\<user>\AppData\Roaming' on Windows.

Configuration is loaded from the default appsettings.json in the application directory, and then overriden by the appsettings.json in the data directory, with additional overrides from envrionment variables and command line.

There is a script in src/Nethermind/Nethermind.BeaconNode.Host/configuration that will convert from the specification YAML files to fragments to insert into the relevant default Production and Development configuration JSON files.

For backwards compatibility, the application can also use the YAML files from the data directory by passing the --config parameter.

### API generation

Controller code, in the Nethermind.BeaconNode.Api project:

```
cd src/Nethermind/Nethermind.BeaconNode.Api
dotnet restore # ensure the tool is installed
dotnet nswag openapi2cscontroller /input:oapi/beacon-node-oapi.yaml /classname:BeaconNodeApi /namespace:Nethermind.BeaconNode.Api /output:BeaconNodeApi-generated.cs /UseLiquidTemplates:true /AspNetNamespace:"Microsoft.AspNetCore.Mvc" /ControllerBaseClass:"Microsoft.AspNetCore.Mvc.Controller"
```

Client code:

```
dotnet nswag openapi2csclient /input:oapi/beacon-node-oapi.yaml /classname:BeaconNodeClient /namespace:Nethermind.BeaconNode.ApiClient /ContractsNamespace:Nethermind.BeaconNode.ApiClient.Contracts /output:../Nethermind.BeaconNode.ApiClient/BeaconNodeClient-generated.cs
```

### Specifications

The Eth 2.0 specifications are evolving. The current code is based on v0.9.1 (2019-11-09, 03fb097)

### Implemented

Implemented so far:

Phase 0:

* The Beacon Chain
* Fork Choice -- main spec implemented; alternate algorithms are not
* Interop Standards in Eth2 PM -- QuickStart and Eth1Data implemented; load from SSZ/YAML is not

Supporting components:

* SimpleSerialize (SSZ) spec -- sufficient to support the beacon chain, no deserialisation yet, see the separate Cortex.Ssz project, https://github.com/NethermindEth/cortex-ssz
* BLS signature verification --  sufficient to support the beacon chain, currently only Windows support, based on the Herumi library, see the separate Cortex.Cryptography.Bls project, https://github.com/NethermindEth/cortex-cryptography-bls

Both these will be merged into the main Nethermind project / renamed / replaced with the Nethermind implementation.

### In Progress

* Honest Validator -- block signing and acceptance by node, so can continuously generate chain
* Eth2 APIs

### To Do

Specification updates:

* v0.9.2 - Clarify Hash as either Root or just Bytes32, validator config, change min validators
* v0.9.3 - Update with separate Signed Envelopes (replace signing root), allowing fork choice merkle filtering

Phase 0:

* Deposit Contract

Phase 1:

* Custody Game
* Shard Data Chains
* Misc beacon chain updates

Light client:

* Sync Protocol
* Merkle Proofs

Networking:

* P2P Interface

Supporting:

* General test format
* Merkle proof formats
* Light client syncing protocol

Other: 

* Eth2 Metrics

Project-specific:

* Installation, e.g.Windows Service, etc (to be intergrated with Nethermind.Runner ?)


## License

Copyright (C) 2019 Demerzel Solutions Limited

This library is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License and GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License and GNU General Public License along with this library. If not, see <https://www.gnu.org/licenses/>.
