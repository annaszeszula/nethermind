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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Nethermind.Blockchain;
using Nethermind.Blockchain.Filters;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.Crypto;
using Nethermind.Facade;
using Nethermind.JsonRpc.Data;
using Nethermind.JsonRpc.Modules.Eth;
using Nethermind.Logging;
using Nethermind.Serialization.Rlp;
using Nethermind.TxPool;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.JsonRpc.Test.Modules
{
    [TestFixture]
    public class EthModuleTests
    {
        private TestRpcBlockchain _test;
        private TestRpcBlockchain _auraTest;

        [SetUp]
        public async Task SetUp()
        {
            _test = await TestRpcBlockchain.ForTest(SealEngineType.NethDev).Build();
            _auraTest = await TestRpcBlockchain.ForTest(SealEngineType.AuRa).Build();
        }

        [TestCase("earliest", "0x3635c9adc5dea00000")]
        [TestCase("latest", "0x3635c9adc5dea00000")]
        [TestCase("pending", "0x3635c9adc5dea00000")]
        [TestCase("0x0", "0x3635c9adc5dea00000")]
        public void Eth_get_balance(string blockParameter, string expectedResult)
        {
            string serialized = _test.TestEthRpc("eth_getBalance", TestItem.AddressA.Bytes.ToHexString(true), blockParameter);
            serialized.Should().Be($"{{\"jsonrpc\":\"2.0\",\"result\":\"{expectedResult}\",\"id\":67}}");
        }

        [Test]
        public void Eth_get_balance_default_block()
        {
            string serialized = _test.TestEthRpc("eth_getBalance", TestItem.AddressA.Bytes.ToHexString(true));
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":\"0x3635c9adc5dea00000\",\"id\":67}}", serialized);
        }

        [TestCase("earliest", "0x0")]
        [TestCase("latest", "0x0")]
        [TestCase("pending", "0x0")]
        [TestCase("0x0", "0x0")]
        public void Eth_get_tx_count(string blockParameter, string expectedResult)
        {
            string serialized = _test.TestEthRpc("eth_getTransactionCount", TestItem.AddressA.Bytes.ToHexString(true), blockParameter);
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":\"{expectedResult}\",\"id\":67}}", serialized);
        }

        [Test]
        public void Eth_get_tx_count_default_block()
        {
            string serialized = _test.TestEthRpc("eth_getTransactionCount", TestItem.AddressA.Bytes.ToHexString(true));
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":\"0x0\",\"id\":67}}", serialized);
        }

        [TestCase("earliest", "0xabcdef")]
        [TestCase("latest", "0xabcdef")]
        [TestCase("pending", "0xabcdef")]
        [TestCase("0x0", "0xabcdef")]
        public void Eth_get_storage_at(string blockParameter, string expectedResult)
        {
            string serialized = _test.TestEthRpc("eth_getStorageAt", TestItem.AddressA.Bytes.ToHexString(true), "0x1", blockParameter);
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":\"{expectedResult}\",\"id\":67}}", serialized);
        }

        [Test]
        public void Eth_get_storage_at_default_block()
        {
            string serialized = _test.TestEthRpc("eth_getStorageAt", TestItem.AddressA.Bytes.ToHexString(true), "0x1");
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":\"0xabcdef\",\"id\":67}}", serialized);
        }

        [Test]
        public void Eth_get_block_number()
        {
            string serialized = _test.TestEthRpc("eth_blockNumber");
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":\"0x9\",\"id\":67}}", serialized);
        }

        [Test]
        public async Task Eth_get_balance_internal_error()
        {
            IBlockchainBridge bridge = Substitute.For<IBlockchainBridge>();
            bridge.Head.Returns((BlockHeader) null);

            _test = await TestRpcBlockchain.ForTest(SealEngineType.NethDev).WithBlockchainBridge(bridge).Build();
            string serialized = _test.TestEthRpc("eth_getBalance", TestItem.AddressA.Bytes.ToHexString(true), "0x01");

            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32603,\"message\":\"Incorrect head block\"},\"id\":67}", serialized);
        }

        [Test]
        public void Eth_get_balance_incorrect_parameters()
        {
            string serialized = _test.TestEthRpc("eth_getBalance", TestItem.KeccakA.Bytes.ToHexString(true), "0x01");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32602,\"message\":\"Invalid params\"},\"id\":67}", serialized);
        }

        [Test]
        public async Task Eth_syncing_true()
        {
            IBlockchainBridge bridge = Substitute.For<IBlockchainBridge>();
            bridge.IsSyncing.Returns(false);
            bridge.Head.Returns(Build.A.BlockHeader.WithNumber(900).TestObject);
            bridge.BestKnown.Returns(1000L);
            bridge.IsSyncing.Returns(true);

            _test = await TestRpcBlockchain.ForTest(SealEngineType.NethDev).WithBlockchainBridge(bridge).Build();

            string serialized = _test.TestEthRpc("eth_syncing");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":{\"startingBlock\":\"0x0\",\"currentBlock\":\"0x384\",\"highestBlock\":\"0x3e8\"},\"id\":67}", serialized);
        }

        [Test]
        public async Task Eth_syncing_false()
        {
            IBlockchainBridge bridge = Substitute.For<IBlockchainBridge>();
            bridge.IsSyncing.Returns(false);
            bridge.Head.Returns(Build.A.BlockHeader.WithNumber(900).TestObject);
            bridge.BestKnown.Returns(1000L);

            _test = await TestRpcBlockchain.ForTest(SealEngineType.NethDev).WithBlockchainBridge(bridge).Build();
            string serialized = _test.TestEthRpc("eth_syncing");

            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":false,\"id\":67}", serialized);
        }

        [Test]
        public async Task Eth_get_filter_logs()
        {
            IBlockchainBridge bridge = Substitute.For<IBlockchainBridge>();
            bridge.GetFilterLogs(Arg.Any<int>()).Returns(new[] {new FilterLog(1, 0, 1, TestItem.KeccakA, 1, TestItem.KeccakB, TestItem.AddressA, new byte[] {1, 2, 3}, new[] {TestItem.KeccakC, TestItem.KeccakD})});
            bridge.FilterExists(1).Returns(true);

            _test = await TestRpcBlockchain.ForTest(SealEngineType.NethDev).WithBlockchainBridge(bridge).Build();
            string serialized = _test.TestEthRpc("eth_getFilterLogs", "0x01");

            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":[{\"address\":\"0xb7705ae4c6f81b66cdb323c65f4e8133690fc099\",\"blockHash\":\"0x03783fac2efed8fbc9ad443e592ee30e61d65f471140c10ca155e937b435b760\",\"blockNumber\":\"0x1\",\"data\":\"0x010203\",\"logIndex\":\"0x1\",\"removed\":false,\"topics\":[\"0x017e667f4b8c174291d1543c466717566e206df1bfd6f30271055ddafdb18f72\",\"0x6c3fd336b49dcb1c57dd4fbeaf5f898320b0da06a5ef64e798c6497600bb79f2\"],\"transactionHash\":\"0x1f675bff07515f5df96737194ea945c36c41e7b4fcef307b7cd4d0e602a69111\",\"transactionIndex\":\"0x1\",\"transactionLogIndex\":\"0x0\"}],\"id\":67}", serialized);
        }

        [TestCase("{}")]
        [TestCase("{\"fromBlock\":\"0x100\",\"toBlock\":\"latest\",\"address\":\"0x00000000000000000001\",\"topics\":[\"0x00000000000000000000000000000001\"]}")]
        [TestCase("{\"fromBlock\":\"earliest\",\"toBlock\":\"pending\",\"address\":[\"0x00000000000000000001\", \"0x00000000000000000001\"],\"topics\":[\"0x00000000000000000000000000000001\", \"0x00000000000000000000000000000002\"]}")]
        [TestCase("{\"topics\":[null, [\"0x00000000000000000000000000000001\", \"0x00000000000000000000000000000002\"]]}")]
        public async Task Eth_get_logs(string parameter)
        {
            IBlockchainBridge bridge = Substitute.For<IBlockchainBridge>();
            bridge.GetLogs(Arg.Any<BlockParameter>(), Arg.Any<BlockParameter>(), Arg.Any<object>(), Arg.Any<IEnumerable<object>>()).Returns(new[] {new FilterLog(1, 0, 1, TestItem.KeccakA, 1, TestItem.KeccakB, TestItem.AddressA, new byte[] {1, 2, 3}, new[] {TestItem.KeccakC, TestItem.KeccakD})});
            bridge.FilterExists(1).Returns(true);

            _test = await TestRpcBlockchain.ForTest(SealEngineType.NethDev).WithBlockchainBridge(bridge).Build();
            string serialized = _test.TestEthRpc("eth_getLogs", parameter);

            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":[{\"address\":\"0xb7705ae4c6f81b66cdb323c65f4e8133690fc099\",\"blockHash\":\"0x03783fac2efed8fbc9ad443e592ee30e61d65f471140c10ca155e937b435b760\",\"blockNumber\":\"0x1\",\"data\":\"0x010203\",\"logIndex\":\"0x1\",\"removed\":false,\"topics\":[\"0x017e667f4b8c174291d1543c466717566e206df1bfd6f30271055ddafdb18f72\",\"0x6c3fd336b49dcb1c57dd4fbeaf5f898320b0da06a5ef64e798c6497600bb79f2\"],\"transactionHash\":\"0x1f675bff07515f5df96737194ea945c36c41e7b4fcef307b7cd4d0e602a69111\",\"transactionIndex\":\"0x1\",\"transactionLogIndex\":\"0x0\"}],\"id\":67}", serialized);
        }

        [Test]
        public void Eth_tx_count_by_hash()
        {
            string serialized = _test.TestEthRpc("eth_getBlockTransactionCountByHash", _test.BlockTree.Genesis.Hash.ToString());
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x0\",\"id\":67}", serialized);
        }

        [Test]
        public void Eth_uncle_count_by_hash()
        {
            string serialized = _test.TestEthRpc("eth_getUncleCountByBlockHash", _test.BlockTree.Genesis.Hash.ToString());
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x0\",\"id\":67}", serialized);
        }

        [TestCase("earliest", "\"0x0\"")]
        [TestCase("latest", "\"0x0\"")]
        [TestCase("pending", "\"0x0\"")]
        [TestCase("0x0", "\"0x0\"")]
        public void Eth_uncle_count_by_number(string blockParameter, string expectedResult)
        {
            string serialized = _test.TestEthRpc("eth_getUncleCountByBlockNumber", blockParameter);
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":{expectedResult},\"id\":67}}", serialized);
        }

        [TestCase("earliest", "\"0x0\"")]
        [TestCase("latest", "\"0x0\"")]
        [TestCase("pending", "\"0x0\"")]
        [TestCase("0x0", "\"0x0\"")]
        public void Eth_tx_count_by_number(string blockParameter, string expectedResult)
        {
            string serialized = _test.TestEthRpc("eth_getBlockTransactionCountByNumber", blockParameter);
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":{expectedResult},\"id\":67}}", serialized);
        }

        [TestCase(false, "{\"jsonrpc\":\"2.0\",\"result\":{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x2167088a0f0de66028d2b728235af6d467108c1750c3e11a8f6e6cd60fddb0e4\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x0\",\"parentHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x201\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0xf4240\",\"timestamp\":\"0xf4240\",\"transactions\":[],\"transactionsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"uncles\":[]},\"id\":67}")]
        [TestCase(true, "{\"jsonrpc\":\"2.0\",\"result\":{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x16af125b31ba6f33725bffd77d8778121c8b24c3c29a9821d2fc15049a5bdcb6\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"number\":\"0x0\",\"parentHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"signature\":\"0x0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"size\":\"0x21b\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"step\":0,\"totalDifficulty\":\"0xf4240\",\"timestamp\":\"0xf4240\",\"transactions\":[],\"transactionsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"uncles\":[]},\"id\":67}")]
        public void Eth_get_block_by_hash(bool aura, string expected)
        {
            TestRpcBlockchain testBlockchain = (aura ? _auraTest : _test);
            string serialized = testBlockchain.TestEthRpc("eth_getBlockByHash", testBlockchain.BlockTree.Genesis.Hash.ToString(), "true");
            Assert.AreEqual(expected, serialized);
        }

        [Test]
        public void Eth_get_block_by_hash_null()
        {
            string serialized = _test.TestEthRpc("eth_getBlockByHash", Keccak.Zero.ToString(), "true");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":null,\"id\":67}", serialized);
        }

        [TestCase("0x0d96929f1193bfc2811d6fb31c4021f7970a5c1d41987f06e1201a2bb65e492f", false, "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x0d96929f1193bfc2811d6fb31c4021f7970a5c1d41987f06e1201a2bb65e492f\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x2ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e2\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x2\",\"parentHash\":\"0xcbb80b69d74f3ea38aa1407ac2f7bab7df6010041a2c8f7e404a2e6696494b29\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x263\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0x2dc6c0\",\"timestamp\":\"0xf4242\",\"transactions\":[\"0x1fb701b713c746b25ac9b0b82345aef86c7541b001ee4c7be4922c71e66e073a\"],\"transactionsRoot\":\"0x2c77d8ab923916632235cd516679013abf16760da5539504bf9405bf1b6e8000\",\"uncles\":[]}")]
        [TestCase("0x0d96929f1193bfc2811d6fb31c4021f7970a5c1d41987f06e1201a2bb65e492f", true, "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x0d96929f1193bfc2811d6fb31c4021f7970a5c1d41987f06e1201a2bb65e492f\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x2ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e2\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x2\",\"parentHash\":\"0xcbb80b69d74f3ea38aa1407ac2f7bab7df6010041a2c8f7e404a2e6696494b29\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x263\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0x2dc6c0\",\"timestamp\":\"0xf4242\",\"transactions\":[{\"hash\":\"0x1fb701b713c746b25ac9b0b82345aef86c7541b001ee4c7be4922c71e66e073a\",\"nonce\":\"0x0\",\"blockHash\":\"0x0d96929f1193bfc2811d6fb31c4021f7970a5c1d41987f06e1201a2bb65e492f\",\"blockNumber\":\"0x2\",\"transactionIndex\":\"0x0\",\"from\":\"0x963e1762be217455aed852e2cbb46053ce0bca98\",\"to\":\"0x0000000000000000000000000000000000000000\",\"value\":\"0x1\",\"gasPrice\":\"0x1\",\"gas\":\"0x5208\",\"data\":\"0x\",\"input\":\"0x\",\"v\":\"0x25\",\"s\":\"0x379b8a0437094d9e1d0ae5c2511d1a4b2cb65be7974d6cbccd5370e14f2df3a5\",\"r\":\"0xac46223b1f2bb2c1a0397d2e44e0cf82b78a766b3035f6c34be06395db18a8e4\"}],\"transactionsRoot\":\"0x2c77d8ab923916632235cd516679013abf16760da5539504bf9405bf1b6e8000\",\"uncles\":[]}")]
        public void Eth_get_block_by_hash_with_tx(string blockParameter, bool withTxData, string expectedResult)
        {
            string serialized = _test.TestEthRpc("eth_getBlockByHash", blockParameter, withTxData.ToString());
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":{expectedResult},\"id\":67}}", serialized, serialized);
        }

        [TestCase("earliest", "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x2167088a0f0de66028d2b728235af6d467108c1750c3e11a8f6e6cd60fddb0e4\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x0\",\"parentHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x201\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0xf4240\",\"timestamp\":\"0xf4240\",\"transactions\":[],\"transactionsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"uncles\":[]}")]
        [TestCase("latest", "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x227ad6224f52f2c4a53c79b2b995980c18951c54c792586619447a313636657b\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x2ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e2\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x9\",\"parentHash\":\"0x8ffe489d2702ea5383e076335f15886ef8313668a142d9eeced777e89e106467\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x201\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0x989680\",\"timestamp\":\"0xf4249\",\"transactions\":[],\"transactionsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"uncles\":[]}")]
        [TestCase("pending", "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x227ad6224f52f2c4a53c79b2b995980c18951c54c792586619447a313636657b\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x2ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e2\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x9\",\"parentHash\":\"0x8ffe489d2702ea5383e076335f15886ef8313668a142d9eeced777e89e106467\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x201\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0x989680\",\"timestamp\":\"0xf4249\",\"transactions\":[],\"transactionsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"uncles\":[]}")]
        [TestCase("0x0", "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x2167088a0f0de66028d2b728235af6d467108c1750c3e11a8f6e6cd60fddb0e4\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x0\",\"parentHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x201\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0xf4240\",\"timestamp\":\"0xf4240\",\"transactions\":[],\"transactionsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"uncles\":[]}")]
        [TestCase("0x20", "null")]
        [TestCase(blockWithTransactions, "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x0d96929f1193bfc2811d6fb31c4021f7970a5c1d41987f06e1201a2bb65e492f\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x2ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e2\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x2\",\"parentHash\":\"0xcbb80b69d74f3ea38aa1407ac2f7bab7df6010041a2c8f7e404a2e6696494b29\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x263\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0x2dc6c0\",\"timestamp\":\"0xf4242\",\"transactions\":[{\"hash\":\"0x1fb701b713c746b25ac9b0b82345aef86c7541b001ee4c7be4922c71e66e073a\",\"nonce\":\"0x0\",\"blockHash\":\"0x0d96929f1193bfc2811d6fb31c4021f7970a5c1d41987f06e1201a2bb65e492f\",\"blockNumber\":\"0x2\",\"transactionIndex\":\"0x0\",\"from\":\"0x963e1762be217455aed852e2cbb46053ce0bca98\",\"to\":\"0x0000000000000000000000000000000000000000\",\"value\":\"0x1\",\"gasPrice\":\"0x1\",\"gas\":\"0x5208\",\"data\":\"0x\",\"input\":\"0x\",\"v\":\"0x25\",\"s\":\"0x379b8a0437094d9e1d0ae5c2511d1a4b2cb65be7974d6cbccd5370e14f2df3a5\",\"r\":\"0xac46223b1f2bb2c1a0397d2e44e0cf82b78a766b3035f6c34be06395db18a8e4\"}],\"transactionsRoot\":\"0x2c77d8ab923916632235cd516679013abf16760da5539504bf9405bf1b6e8000\",\"uncles\":[]}")]
        public void Eth_get_block_by_number(string blockParameter, string expectedResult)
        {
            string serialized = _test.TestEthRpc("eth_getBlockByNumber", blockParameter, "true");
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":{expectedResult},\"id\":67}}", serialized, serialized);
        }

        private const string blockWithTransactions = "0x02";

        [TestCase("earliest", "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x2167088a0f0de66028d2b728235af6d467108c1750c3e11a8f6e6cd60fddb0e4\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x0\",\"parentHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x201\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0xf4240\",\"timestamp\":\"0xf4240\",\"transactions\":[],\"transactionsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"uncles\":[]}")]
        [TestCase("latest", "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x227ad6224f52f2c4a53c79b2b995980c18951c54c792586619447a313636657b\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x2ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e2\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x9\",\"parentHash\":\"0x8ffe489d2702ea5383e076335f15886ef8313668a142d9eeced777e89e106467\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x201\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0x989680\",\"timestamp\":\"0xf4249\",\"transactions\":[],\"transactionsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"uncles\":[]}")]
        [TestCase("pending", "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x227ad6224f52f2c4a53c79b2b995980c18951c54c792586619447a313636657b\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x2ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e2\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x9\",\"parentHash\":\"0x8ffe489d2702ea5383e076335f15886ef8313668a142d9eeced777e89e106467\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x201\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0x989680\",\"timestamp\":\"0xf4249\",\"transactions\":[],\"transactionsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"uncles\":[]}")]
        [TestCase("0x0", "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x2167088a0f0de66028d2b728235af6d467108c1750c3e11a8f6e6cd60fddb0e4\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x0\",\"parentHash\":\"0x0000000000000000000000000000000000000000000000000000000000000000\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x201\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0xf4240\",\"timestamp\":\"0xf4240\",\"transactions\":[],\"transactionsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"uncles\":[]}")]
        [TestCase("0x20", "null")]
        [TestCase(blockWithTransactions, "{\"difficulty\":\"0xf4240\",\"extraData\":\"0x010203\",\"gasLimit\":\"0x3d0900\",\"gasUsed\":\"0x0\",\"hash\":\"0x0d96929f1193bfc2811d6fb31c4021f7970a5c1d41987f06e1201a2bb65e492f\",\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"miner\":\"0x0000000000000000000000000000000000000000\",\"mixHash\":\"0x2ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e2\",\"nonce\":\"0x00000000000003e8\",\"number\":\"0x2\",\"parentHash\":\"0xcbb80b69d74f3ea38aa1407ac2f7bab7df6010041a2c8f7e404a2e6696494b29\",\"receiptsRoot\":\"0x56e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421\",\"sha3Uncles\":\"0x1dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347\",\"size\":\"0x263\",\"stateRoot\":\"0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f\",\"totalDifficulty\":\"0x2dc6c0\",\"timestamp\":\"0xf4242\",\"transactions\":[\"0x1fb701b713c746b25ac9b0b82345aef86c7541b001ee4c7be4922c71e66e073a\"],\"transactionsRoot\":\"0x2c77d8ab923916632235cd516679013abf16760da5539504bf9405bf1b6e8000\",\"uncles\":[]}")]
        public void Eth_get_block_by_number_no_details(string blockParameter, string expectedResult)
        {
            string serialized = _test.TestEthRpc("eth_getBlockByNumber", blockParameter, "false");
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":{expectedResult},\"id\":67}}", serialized);

            string serialized2 = _test.TestEthRpc("eth_getBlockByNumber", blockParameter);
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":{expectedResult},\"id\":67}}", serialized2, serialized2);
        }

        [Test]
        public void Eth_get_block_by_number_null()
        {
            string serialized = _test.TestEthRpc("eth_getBlockByNumber", "1000000", "false");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":null,\"id\":67}", serialized);
        }

        [Test]
        public void Eth_get_code()
        {
            string serialized = _test.TestEthRpc("eth_getCode", TestItem.AddressA.ToString(), "latest");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0xabcd\",\"id\":67}", serialized);
        }

        [Test]
        public void Eth_get_code_default()
        {
            string serialized = _test.TestEthRpc("eth_getCode", TestItem.AddressA.ToString());
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0xabcd\",\"id\":67}", serialized);
        }

        [Test]
        public void Eth_call_web3_sample()
        {
            var transaction = _test.JsonSerializer.Deserialize<TransactionForRpc>("{\"data\": \"0x70a082310000000000000000000000006c1f09f6271fbe133db38db9c9280307f5d22160\", \"to\": \"0x0d8775f648430679a709e98d2b0cb6250d2887ef\"}");
            string serialized = _test.TestEthRpc("eth_call", _test.JsonSerializer.Serialize(transaction), "0x0");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x\",\"id\":67}", serialized);
        }

        [Test]
        public void Eth_call_web3_sample_not_enough_gas_system_account()
        {
            _test.State.AccountExists(Address.SystemUser).Should().BeFalse();
            var transaction = _test.JsonSerializer.Deserialize<TransactionForRpc>("{\"gasPrice\":\"0x100000\", \"data\": \"0x70a082310000000000000000000000006c1f09f6271fbe133db38db9c9280307f5d22160\", \"to\": \"0x0d8775f648430679a709e98d2b0cb6250d2887ef\"}");
            string serialized = _test.TestEthRpc("eth_call", _test.JsonSerializer.Serialize(transaction), "0x0");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x\",\"id\":67}", serialized);
            _test.State.AccountExists(Address.SystemUser).Should().BeFalse();
        }

        [Test]
        public void Eth_call_web3_sample_not_enough_gas_other_account()
        {
            Address someAccount = new Address("0x0001020304050607080910111213141516171819");
            _test.State.AccountExists(someAccount).Should().BeFalse();
            var transaction = _test.JsonSerializer.Deserialize<TransactionForRpc>("{\"from\":\"0x0001020304050607080910111213141516171819\",\"gasPrice\":\"0x100000\", \"data\": \"0x70a082310000000000000000000000006c1f09f6271fbe133db38db9c9280307f5d22160\", \"to\": \"0x0d8775f648430679a709e98d2b0cb6250d2887ef\"}");
            string serialized = _test.TestEthRpc("eth_call", _test.JsonSerializer.Serialize(transaction), "0x0");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x\",\"id\":67}", serialized);
            _test.State.AccountExists(someAccount).Should().BeFalse();
        }

        [Test]
        public void Eth_estimateGas_web3_sample_not_enough_gas_system_account()
        {
            _test.State.AccountExists(Address.SystemUser).Should().BeFalse();
            var transaction = _test.JsonSerializer.Deserialize<TransactionForRpc>("{\"gasPrice\":\"0x100000\", \"data\": \"0x70a082310000000000000000000000006c1f09f6271fbe133db38db9c9280307f5d22160\", \"to\": \"0x0d8775f648430679a709e98d2b0cb6250d2887ef\"}");
            string serialized = _test.TestEthRpc("eth_estimateGas", _test.JsonSerializer.Serialize(transaction));
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x5898\",\"id\":67}", serialized);
            _test.State.AccountExists(Address.SystemUser).Should().BeFalse();
        }

        [Test]
        public void Eth_estimateGas_web3_sample_not_enough_gas_other_account()
        {
            Address someAccount = new Address("0x0001020304050607080910111213141516171819");
            _test.State.AccountExists(someAccount).Should().BeFalse();
            var transaction = _test.JsonSerializer.Deserialize<TransactionForRpc>("{\"from\":\"0x0001020304050607080910111213141516171819\",\"gasPrice\":\"0x100000\", \"data\": \"0x70a082310000000000000000000000006c1f09f6271fbe133db38db9c9280307f5d22160\", \"to\": \"0x0d8775f648430679a709e98d2b0cb6250d2887ef\"}");
            string serialized = _test.TestEthRpc("eth_estimateGas", _test.JsonSerializer.Serialize(transaction));
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x5898\",\"id\":67}", serialized);
            _test.State.AccountExists(someAccount).Should().BeFalse();
        }

        [Test]
        public void Eth_estimateGas_web3_above_block_gas_limit()
        {
            Address someAccount = new Address("0x0001020304050607080910111213141516171819");
            _test.State.AccountExists(someAccount).Should().BeFalse();
            var transaction = _test.JsonSerializer.Deserialize<TransactionForRpc>("{\"from\":\"0x0001020304050607080910111213141516171819\",\"gas\":\"0x100000000\",\"gasPrice\":\"0x100000\", \"data\": \"0x70a082310000000000000000000000006c1f09f6271fbe133db38db9c9280307f5d22160\", \"to\": \"0x0d8775f648430679a709e98d2b0cb6250d2887ef\"}");
            string serialized = _test.TestEthRpc("eth_estimateGas", _test.JsonSerializer.Serialize(transaction));
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x5898\",\"id\":67}", serialized);
            _test.State.AccountExists(someAccount).Should().BeFalse();
        }

        [Test]
        public void Eth_call_no_sender()
        {
            var transaction = new TransactionForRpc(Keccak.Zero, 1L, 1, new Transaction());
            transaction.To = TestItem.AddressB;

            string serialized = _test.TestEthRpc("eth_call", _test.JsonSerializer.Serialize(transaction), "latest");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x\",\"id\":67}", serialized);
        }

        [Test]
        public void Eth_call_no_recipient_should_work_as_init()
        {
            var transaction = new TransactionForRpc(Keccak.Zero, 1L, 1, new Transaction());
            transaction.From = TestItem.AddressA;
            transaction.Data = new byte[] {1, 2, 3};

            string serialized = _test.TestEthRpc("eth_call", _test.JsonSerializer.Serialize(transaction), "latest");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32015,\"message\":\"VM execution error.\",\"data\":\"StackUnderflow\"},\"id\":67}", serialized);
        }

        [Test]
        public void Eth_call_ethereum_recipient()
        {
            string serialized = _test.TestEthRpc("eth_call", "{\"data\":\"0x12\",\"from\":\"0x7301cfa0e1756b71869e93d4e4dca5c7d0eb0aa6\",\"to\":\"ethereum\"}", "latest");
            Assert.True(serialized.StartsWith("{\"jsonrpc\":\"2.0\",\"error\""));
        }

        [Test]
        public void Eth_call_ok()
        {
            var transaction = new TransactionForRpc(Keccak.Zero, 1L, 1, new Transaction());
            transaction.From = TestItem.AddressA;
            transaction.To = TestItem.AddressB;

            string serialized = _test.TestEthRpc("eth_call", _test.JsonSerializer.Serialize(transaction), "latest");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x\",\"id\":67}", serialized);
        }

        [Test]
        public void Eth_call_missing_state_after_fast_sync()
        {
            var transaction = new TransactionForRpc(Keccak.Zero, 1L, 1, new Transaction());
            transaction.From = TestItem.AddressA;
            transaction.To = TestItem.AddressB;

            _test.StateDb.Clear();

            string serialized = _test.TestEthRpc("eth_call", _test.JsonSerializer.Serialize(transaction), "latest");
            serialized.Should().StartWith("{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32002,");
        }

        [Test]
        public void Eth_get_block_by_number_with_number_bad_number()
        {
            string serialized = _test.TestEthRpc("eth_getBlockByNumber", "'0x1234567890123456789012345678901234567890123456789012345678901234567890'", "true");
            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32602,\"message\":\"Invalid params\"},\"id\":67}", serialized);
        }

        [Test]
        public void Eth_get_block_by_number_empty_param()
        {
            string serialized = _test.TestEthRpc("eth_getBlockByNumber", "", "true");
            Assert.True(serialized.StartsWith("{\"jsonrpc\":\"2.0\",\"error\""));
        }

        [Test]
        public async Task Eth_get_transaction_receipt()
        {
            IBlockchainBridge bridge = Substitute.For<IBlockchainBridge>();
            var entries = new[]
            {
                Build.A.LogEntry.TestObject,
                Build.A.LogEntry.TestObject
            };
            bridge.GetReceipt(Arg.Any<Keccak>()).Returns(Build.A.Receipt.WithBloom(new Bloom(entries, new Bloom())).WithAllFieldsFilled.WithLogs(entries).TestObject);

            _test = await TestRpcBlockchain.ForTest(SealEngineType.NethDev).WithBlockchainBridge(bridge).Build();
            string serialized = _test.TestEthRpc("eth_getTransactionReceipt", TestItem.KeccakA.ToString());

            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":{\"transactionHash\":\"0x03783fac2efed8fbc9ad443e592ee30e61d65f471140c10ca155e937b435b760\",\"transactionIndex\":\"0x2\",\"blockHash\":\"0x017e667f4b8c174291d1543c466717566e206df1bfd6f30271055ddafdb18f72\",\"blockNumber\":\"0x2\",\"cumulativeGasUsed\":\"0x3e8\",\"gasUsed\":\"0x64\",\"from\":\"0xb7705ae4c6f81b66cdb323c65f4e8133690fc099\",\"to\":\"0x942921b14f1b1c385cd7e0cc2ef7abe5598c8358\",\"contractAddress\":\"0x76e68a8696537e4141926f3e528733af9e237d69\",\"logs\":[{\"removed\":false,\"logIndex\":\"0x0\",\"transactionIndex\":\"0x2\",\"transactionHash\":\"0x03783fac2efed8fbc9ad443e592ee30e61d65f471140c10ca155e937b435b760\",\"blockHash\":\"0x017e667f4b8c174291d1543c466717566e206df1bfd6f30271055ddafdb18f72\",\"blockNumber\":\"0x2\",\"address\":\"0x0000000000000000000000000000000000000000\",\"data\":\"0x\",\"topics\":[\"0x0000000000000000000000000000000000000000000000000000000000000000\"]},{\"removed\":false,\"logIndex\":\"0x1\",\"transactionIndex\":\"0x2\",\"transactionHash\":\"0x03783fac2efed8fbc9ad443e592ee30e61d65f471140c10ca155e937b435b760\",\"blockHash\":\"0x017e667f4b8c174291d1543c466717566e206df1bfd6f30271055ddafdb18f72\",\"blockNumber\":\"0x2\",\"address\":\"0x0000000000000000000000000000000000000000\",\"data\":\"0x\",\"topics\":[\"0x0000000000000000000000000000000000000000000000000000000000000000\"]}],\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"root\":\"0x1f675bff07515f5df96737194ea945c36c41e7b4fcef307b7cd4d0e602a69111\",\"status\":\"0x1\",\"error\":\"error\"},\"id\":67}", serialized);
        }

        [Test]
        public void Eth_get_transaction_receipt_returns_null_on_missing_receipt()
        {
            IBlockchainBridge bridge = Substitute.For<IBlockchainBridge>();

            bridge.GetReceipt(Arg.Any<Keccak>()).Returns((TxReceipt) null);

            IEthModule module = new EthModule(new JsonRpcConfig(), LimboLogs.Instance, bridge);

            string serialized = _test.TestEthRpc("eth_getTransactionReceipt", TestItem.KeccakA.ToString());

            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":null,\"id\":67}", serialized);
        }


        [Test]
        public async Task Eth_syncing()
        {
            IBlockchainBridge bridge = Substitute.For<IBlockchainBridge>();
            bridge.IsSyncing.Returns(true);
            bridge.BestKnown.Returns(6178000L);
            bridge.Head.Returns(Build.A.BlockHeader.WithNumber(6170000L).TestObject);

            _test = await TestRpcBlockchain.ForTest(SealEngineType.NethDev).WithBlockchainBridge(bridge).Build();
            string serialized = _test.TestEthRpc("eth_syncing");

            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":{\"startingBlock\":\"0x0\",\"currentBlock\":\"0x5e2590\",\"highestBlock\":\"0x5e44d0\"},\"id\":67}", serialized);
        }

        [Test]
        public void Eth_chain_id()
        {
            IBlockchainBridge bridge = Substitute.For<IBlockchainBridge>();
            bridge.GetChainId().Returns(1);

            IEthModule module = new EthModule(new JsonRpcConfig(), LimboLogs.Instance, bridge);

            string serialized = _test.TestEthRpc("eth_chainid");

            Assert.AreEqual("{\"jsonrpc\":\"2.0\",\"result\":\"0x1\",\"id\":67}", serialized);
        }

        [Test]
        public async Task Send_transaction_with_signature_will_not_try_to_sign()
        {
            IBlockchainBridge bridge = Substitute.For<IBlockchainBridge>();
            bridge.SendTransaction(null, TxHandlingOptions.PersistentBroadcast).ReturnsForAnyArgs(TestItem.KeccakA);

            _test = await TestRpcBlockchain.ForTest(SealEngineType.NethDev).WithBlockchainBridge(bridge).Build();
            Transaction tx = Build.A.Transaction.Signed(new EthereumEcdsa(MainNetSpecProvider.Instance, LimboLogs.Instance), TestItem.PrivateKeyA, 10000000).TestObject;
            string serialized = _test.TestEthRpc("eth_sendRawTransaction", Rlp.Encode(tx, RlpBehaviors.None).Bytes.ToHexString());

            bridge.DidNotReceiveWithAnyArgs().Sign(null);
            Assert.AreEqual($"{{\"jsonrpc\":\"2.0\",\"result\":\"{TestItem.KeccakA.Bytes.ToHexString(true)}\",\"id\":67}}", serialized);
        }
    }
}