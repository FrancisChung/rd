﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Rd;
using JetBrains.Rd.Base;
using JetBrains.Rd.Impl;
using JetBrains.Rd.Reflection;
using JetBrains.Serialization;
using JetBrains.Threading;
using NUnit.Framework;

#if NET35

#endif

namespace Test.RdFramework.Reflection
{
  [TestFixture]
  public class ProxyGeneratorAsyncCallsTest : RdReflectionTestBase
  {
    public class AsyncTestFixture<T>
    {
      public virtual int Rounds => 1;

      public virtual void Client(int round) { }
      public virtual void Server(int round) { }
    }

#if NET35
    private static TaskHack Task = new TaskHack();
#endif

    [RdRpc]
    public interface IAsyncCallsTest
    {
      Task<string> GetStringAsync();
      Task RunSomething();

      Task<int> iSum(int a, int b);
      Task<ulong> ulSum(ulong a, ulong b);
      Task<short> sSum(short a, short b);
      Task<ushort> usSum(ushort a, ushort b);
      Task<byte> bSum(byte a, byte b);
      Task<uint> uiSum(uint a, uint b);
      Task<long> lSum(long a, long b);
    }

    [RdExt]
    internal class AsyncCallsTest : RdExtReflectionBindableBase, IAsyncCallsTest
    {
      public Task<string> GetStringAsync()
      {
        return Task.FromResult("result");
      }

      public Task RunSomething()
      {
        return Task.CompletedTask;
      }

      public Task<int> iSum(int a, int b) => Task.FromResult(a + b);
      public Task<uint> uiSum(uint a, uint b) => Task.FromResult(a + b);
      public Task<long> lSum(long a, long b) => Task.FromResult(a + b);
      public Task<ulong> ulSum(ulong a, ulong b) => Task.FromResult(a + b);
      public Task<short> sSum(short a, short b) => Task.FromResult(unchecked((short) (a + b)));
      public Task<ushort> usSum(ushort a, ushort b) => Task.FromResult(unchecked((ushort) (a + b)));
      public Task<byte> bSum(byte a, byte b) => Task.FromResult(unchecked((byte) (a + b)));
    }

    [RdRpc]
    public interface IAsyncModelsTestDifferentName
    {
      Task<AColor> QueryColor();
      void SetPath(AsyncModelsTest.FileSystemPath animal);
    }

    [RdExt]
    public class AsyncModelsTest : RdExtReflectionBindableBase, IAsyncModelsTestDifferentName
    {
      public async Task<AColor> QueryColor()
      {
        return await Task.FromResult(new AColor(10, 10, 10));
      }

      public void SetPath(FileSystemPath animal)
      {
        Assert.True(!animal.Path.Any(char.IsUpper), "!animal.Path.Any(char.IsUpper)");
      }

      [RdScalar]
      public class FileSystemPath
      {
        private string myPath;

        public string Path => myPath;

        public FileSystemPath(string path)
        {
          myPath = path;
        }

        public static FileSystemPath Read(SerializationCtx ctx, UnsafeReader reader)
        {
          return new FileSystemPath(reader.ReadString());
        }

        public static void Write(SerializationCtx ctx, UnsafeWriter writer, FileSystemPath value)
        {
          writer.Write(value.myPath.ToLowerInvariant());
        }
      }
    }

    [RdScalar] // not required
    public class AColor
    {
      public AColor(int r, int g, int b)
      {
        R = r;
        G = g;
        B = b;
      }
      public int R;
      public int G;
      public int B;
    }

    [RdRpc]
    public interface ISyncCallsTest
    {
      RdList<string> History { get; }
      string Concat(string a, string b, string c);
    }

    [RdExt]
    public class SyncCallsTest : RdExtReflectionBindableBase, ISyncCallsTest
    {
      public RdList<string> History { get; }

      public string Concat(string a, string b, string c)
      {
        var result = string.Concat(a, b, c);
        History.Add(result);
        return result;
      }
    }

    [Test]
    public void TestAsync()
    {
      string result = null;
      TestAsyncCalls(model =>
      {
        model.GetStringAsync().ContinueWith(t => result = t.Result, TaskContinuationOptions.ExecuteSynchronously);
      });

#if NET35
      // it seems like ExecuteSynchronously does not work in NET35
      SpinWaitEx.SpinUntil(TimeSpan.FromSeconds(1), () => result != null);
#endif
      Assert.AreEqual(result, "result");
    }

    [Test]
    public void TestAsyncVoid()
    {
      // todo: really check long running task result
      TestAsyncCalls(model => { model.RunSomething(); });
    }

    [Test]
    public void TestAsyncModels()
    {
      TestTemplate<AsyncModelsTest, IAsyncModelsTestDifferentName>(proxy =>
      {
        proxy.SetPath(new AsyncModelsTest.FileSystemPath("C:\\hello"));
        var queryColor = proxy.QueryColor();
        Assert.AreEqual(queryColor.Result.R, 10);
        Assert.AreEqual(queryColor.Result.G, 10);
        Assert.AreEqual(queryColor.Result.B, 10);
      });
    }

    [Test] public void TestAsyncSum1() => TestAsyncCalls(model => Assert.AreEqual(model.iSum(100, -150).Result, -50));
    [Test] public void TestAsyncSum2() => TestAsyncCalls(model => Assert.AreEqual(model.uiSum(uint.MaxValue, 0).Result, uint.MaxValue));
    [Test] public void TestAsyncSum3() => TestAsyncCalls(model => Assert.AreEqual(model.sSum(100, -150).Result, -50));
    [Test] public void TestAsyncSum4() => TestAsyncCalls(model => Assert.AreEqual(model.usSum(ushort.MaxValue, 1).Result, 0));
    [Test] public void TestAsyncSum5() => TestAsyncCalls(model => Assert.AreEqual(model.lSum(long.MaxValue, 0).Result, long.MaxValue));
    [Test] public void TestAsyncSum6() => TestAsyncCalls(model => Assert.AreEqual(model.ulSum(ulong.MaxValue, 0).Result, ulong.MaxValue));
    [Test] public void TestAsyncSum7() => TestAsyncCalls(model => Assert.AreEqual(model.bSum(byte.MaxValue, 1).Result, 0));

    [Test, Description("Sync call in and asynchonous enviroment")]
    public void TestSyncCall()
    {
      TestSyncCalls(m =>
      {
        CollectionAssert.IsEmpty(m.History);
        m.Concat("1", "2", "3");
        CollectionAssert.AreEqual(m.History, new[] {"123"});
      });
    }


    private void TestAsyncCalls(Action<IAsyncCallsTest> run) => TestTemplate<AsyncCallsTest, IAsyncCallsTest>(run);
    private void TestSyncCalls(Action<ISyncCallsTest> run) => TestTemplate<SyncCallsTest, ISyncCallsTest>(run);

    protected void TestTemplate<TImpl, TInterface>(Action<TInterface> runTest) where TImpl : RdBindableBase where TInterface : class
    {
      ClientProtocol.Scheduler.Queue(() =>
      {
        var client = CFacade.Activator.ActivateBind<TImpl>(TestLifetime, ClientProtocol);
      });

      TInterface proxy = null;
      ServerProtocol.Scheduler.Queue(() => { proxy = SFacade.ActivateProxy<TInterface>(TestLifetime, ServerProtocol); });

      WaitMessages();

      using (var barrier = new ManualResetEvent(false))
      {
        ServerProtocol.Scheduler.Queue(() =>
          Assertion.Assert((proxy as RdExtReflectionBindableBase).NotNull().Connected.Value, "((RdReflectionBindableBase)proxy).Connected.Value"));
        runTest(proxy);
        ServerProtocol.Scheduler.Queue(() => barrier.Set());

        WaitMessages();
        barrier.WaitOne();
      }
    }

    private void WaitMessages()
    {
      bool IsIdle(IRdDynamic p) => ((SingleThreadScheduler) p.Proto.Scheduler).IsIdle;
      SpinWaitEx.SpinUntil(() => IsIdle(ServerProtocol) && IsIdle(ClientProtocol));
    }

    protected override IScheduler CreateScheduler(bool isServer)
    {
      string name = (isServer ? "server" : "client") + " scheduler";
      IScheduler result = null;

      var thread = new Thread(() => SingleThreadScheduler.RunInCurrentStackframe(TestLifetime, name, s => result = s)) { Name = name };
      thread.Start();
      SpinWaitEx.SpinUntil(() => result != null);
      return result;
    }
  }

}