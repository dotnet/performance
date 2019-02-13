﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Microsoft.ML.Data;
using Microsoft.ML.Internal.Calibration;
using Microsoft.ML.Learners;

namespace Microsoft.ML.Benchmarks
{
    /// <summary>
    /// This is an end-to-end benchmark that measures performance of a complete ML pipeline.
    /// The pipeline consists of applying one hot encoding to categorical features, normalizing
    /// numerical features, training a KMeans model on the features thus derived, and finally
    /// training a Logistic Regression model on the derived features plus the score from the
    /// KMeans trainer.
    /// </summary>
    public class KMeansAndLogisticRegressionBench
    {
        private readonly string _dataPath = Program.GetInvariantCultureDataPath("adult.tiny.with-schema.txt");

        [Benchmark]
        public ParameterMixingCalibratedPredictor TrainKMeansAndLR()
        {
            var ml = new MLContext(seed: 1);
            // Pipeline

            var input = ml.Data.ReadFromTextFile(_dataPath, new[] {
                            new TextLoader.Column("Label", DataKind.BL, 0),
                            new TextLoader.Column("CatFeatures", DataKind.TX,
                                new [] {
                                    new TextLoader.Range() { Min = 1, Max = 8 },
                                }),
                            new TextLoader.Column("NumFeatures", DataKind.R4,
                                new [] {
                                    new TextLoader.Range() { Min = 9, Max = 14 },
                                }),
            }, hasHeader: true);

            var estimatorPipeline = ml.Transforms.Categorical.OneHotEncoding("CatFeatures")
                .Append(ml.Transforms.Normalize("NumFeatures"))
                .Append(ml.Transforms.Concatenate("Features", "NumFeatures", "CatFeatures"))
                .Append(ml.Clustering.Trainers.KMeans("Features"))
                .Append(ml.Transforms.Concatenate("Features", "Features", "Score"))
                .Append(ml.BinaryClassification.Trainers.LogisticRegression(
                    new LogisticRegression.Options { EnforceNonNegativity = true, OptTol = 1e-3f, }));

            var model = estimatorPipeline.Fit(input);
            // Return the last model in the chain.
            return model.LastTransformer.Model;
        }
    }
}