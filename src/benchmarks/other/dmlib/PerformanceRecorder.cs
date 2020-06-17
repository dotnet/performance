// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AzDataMovementBenchmark
{
    public class PerformanceRecorder
    {
        public static TimeSpan DefaultPeriod { get; set; } = TimeSpan.FromMilliseconds(100);

        public Stream Stream { get; private set; }
        public DateTime Begin {get; private set;}
        public DateTime End { get; private set; }
        public long Bytes { get; set; } = 0;
        public long MBits { get => Bytes * 8 / 1000 /1000; }
        public double Seconds { get => (End - Begin).TotalSeconds; }
        public double AvgMbps { get => MBits / Seconds; }
        public List<PerformanceRecord> Records { get; private set; } = new List<PerformanceRecord>();
        public TimeSpan Period { get; set; }

        private CancellationTokenSource cancel = null;
        private TimeSpan previousTime; // Redundant, but explicit
        private long previousBytes = 0;
        private Task recorderTask = null;
        private Stopwatch timer = new Stopwatch();

        public PerformanceRecorder(Stream stream = null, TimeSpan? period = null) {
            Stream = stream;
            if (period != null) {
                Period = period.Value;
            }
            else {
                Period = DefaultPeriod;
            }
        }

        public void Start() {
            timer.Start();
            Begin = DateTime.UtcNow;

            if (Stream != null) {
                cancel = new CancellationTokenSource();
                recorderTask = Record(Period, cancel.Token);

                previousTime = timer.Elapsed;
                previousBytes = Stream.Length;
            }
        }

        public void Stop() {
            End = DateTime.UtcNow;
            timer.Stop();

            if (Stream != null) {
                cancel.Cancel();
                recorderTask.Wait();
            }
        }

        // Merge data from another PerformanceRecord into this one
        public void Merge(PerformanceRecorder other) {
            // Time offset to apply to other's Records in merging
            var offset = this.Begin - other.Begin;

            this.Begin = DateTime.Compare(this.Begin, other.Begin) < 0 ? this.Begin : other.Begin;
            this.End = DateTime.Compare(this.End, other.End) > 0 ? this.End : other.End;
            this.Bytes += other.Bytes;

            // Track position in this Records list
            int j = 0;
            for (int i=0; i <= this.Records.Count; i++) {
                  while (j < other.Records.Count && (i >= this.Records.Count || this.Records[i].Time > (other.Records[j].Time + offset))) {
                      this.Records.Insert(i,
                          new PerformanceRecord{
                              Time=other.Records[j].Time + offset,
                              ReceivedBytes = other.Records[j].ReceivedBytes
                          }
                      );
                      i++; // Account for the new record
                      j++;
                  }
            }
        }

        // Calculates the simple moving average throughput reported at a period and window
        // period defaults to 6ms and window to 12ms
        public List<double> ThroughputMovingAverage(uint period=100, uint window=500) {
            var result = new List<double>();

            // The head and tail of the window
            int tail = 0, head = 1;
            uint start = 0;
            while (tail < Records.Count) {
                // Check if the head needs to advance to expand the window
                if (head < Records.Count && Records[head].Time.TotalMilliseconds - start < window) {
                    head++;
                }
                // Take an average of the windows then advance the tail by period
                else {
                    long total = 0;
                    for (int i=tail; i<head; i++) {
                        total += Records[i].ReceivedBytes;
                    }
                    result.Add((total * 8) / (window / 1000.0));

                    start += period;
                    while (tail < Records.Count && Records[tail].Time.TotalMilliseconds < start) {
                        tail++;
                    }
                }
            }

            return result;
        }

        private PerformanceRecord Sample() {
            var time = timer.Elapsed;
            long bytes = 0;
            if (Stream != null) {
                bytes = Stream.Length;
            }

            var record = new PerformanceRecord {
                Time = time,
                ReceivedBytes = bytes - previousBytes
            };

            previousTime = time;
            previousBytes = bytes;

            return record;
        }

        private async Task Record(TimeSpan period, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try {
                    await Task.Delay(period, token);
                }
                catch (TaskCanceledException) {
                    return;
                }

                if (!token.IsCancellationRequested) {
                    Records.Add(Sample());
                }
            }
        }
    }

    // The time and number of bytes arrived as if all data arrived at once
    public class PerformanceRecord
    {
        public TimeSpan Time { get; set; }
        public long ReceivedBytes { get; set; }
    }
}
