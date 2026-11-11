using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using GcLib.Benchmarks;

// BenchmarkRunner is the entry point for running benchmarks. It discovers all benchmark classes in the assembly and executes them, providing detailed performance metrics and analysis.
//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

var config = new BenchmarkDotNet.Configs.ManualConfig()
    .AddJob(Job.Dry)
    .AddLogger(BenchmarkDotNet.Loggers.ConsoleLogger.Default)
    .AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance);
    
var summary = BenchmarkRunner.Run<BufferAcquisitionBenchmarks>(config);