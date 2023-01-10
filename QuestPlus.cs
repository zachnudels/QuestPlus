using System;
using System.Collections.Generic;
using MathNet.Numerics; // https://github.com/GlitchEnzo/NuGetForUnity for unity install
using System.Linq;
using System.IO;
//using UnityEngine;

namespace test {
    using ResponseMap = Dictionary<int, Dictionary<double, Dictionary<ParameterSet, double>>>;
    using IntensityMap = Dictionary<double, Dictionary<ParameterSet, double>>;
    using ParameterMap = Dictionary<ParameterSet, double>;

    using InputMap = Dictionary<int, Dictionary<double, double>>;

    public class QuestPlus { // : MonoBehaviour {

        public double stimMin;
        public double stimMax;
        public double stimStep;

        public double guessMin;
        public double guessMax;
        public double guessStep;

        public double lapseMin;
        public double lapseMax;
        public double lapseStep;

        public double meanMin;
        public double meanMax;
        public double meanStep;

        public double sdMin;
        public double sdMax;
        public double sdStep;
        ResponseMap possible_posterior;

        static Random rnd = new Random();


        List<double> stimSamples;
        List<List<double>> parmSamples;

        public ParameterMap prior;
        public ParameterMap posterior;
        // Map of likelihoods where keys are (in order): response, intensity, parameterSet
        ResponseMap likelihoodMap;

        List<double> intensityHistory;
        List<int> responseHistory;

        public QuestPlus(double stimMin,
         double stimMax,
         double stimStep,

         double guessMin,
         double guessMax,
         double guessStep,

         double lapseMin,
         double lapseMax,
         double lapseStep,

         double meanMin,
         double meanMax,
         double meanStep,

         double sdMin,
         double sdMax,
         double sdStep
        ) {
            stimSamples = Range(stimMin, stimMax, stimStep);
            parmSamples = new List<List<double>>();
            parmSamples.Add(Range(guessMin, guessMax, guessStep));
            parmSamples.Add(Range(lapseMin, lapseMax, lapseStep));
            parmSamples.Add(Range(meanMin, meanMax, meanStep));
            parmSamples.Add(Range(sdMin, sdMax, sdStep));

            posterior = GenPriorList(parmSamples);
            likelihoodMap = GenLikelihoods();
            intensityHistory = new List<double>();
            responseHistory = new List<int>();

        }

        double PsychNormCdf(double intensity, ParameterSet parms) {
            MathNet.Numerics.Distributions.Normal result = new MathNet.Numerics.Distributions.Normal(parms.mean, parms.sd);
            return parms.guess + (1 - parms.guess - parms.lapse) * result.CumulativeDistribution(intensity);
        }

        ParameterMap GenPriorList(List<List<double>> priorRanges) {

            /*
             * Generate a map corresponding to a uniform prior over the ND stimulus paramters
             */
            double prod = 1.0 / priorRanges.Aggregate(1, (acc, x) => acc * x.Count);
            ParameterMap prior = new ParameterMap();
            foreach (var parms in CartesianProduct(parmSamples)) {
                ParameterSet parmSet = new ParameterSet(parms.ToArray());
                prior.Add(parmSet, prod);
            }
            return prior;
        }

        ResponseMap GenLikelihoods() {
            ResponseMap likelihoodMap = new ResponseMap();
            IntensityMap likelihoodMap_0 = new IntensityMap();
            IntensityMap likelihoodMap_1 = new IntensityMap();

            foreach (var intensity in stimSamples) {
                ParameterMap likelihoods_0 = new ParameterMap();
                ParameterMap likelihoods_1 = new ParameterMap();

                foreach (var parms in CartesianProduct(parmSamples)) {
                    ParameterSet parmSet = new ParameterSet(parms.ToArray());
                    double likelihood_1 = PsychNormCdf(intensity, parmSet); // Likelihood of being correct
                    double likelihood_0 = 1 - likelihood_1; // Likelihood of being incorrect
                    likelihoods_0.Add(parmSet, likelihood_0);
                    likelihoods_1.Add(parmSet, likelihood_1);


                }

                likelihoodMap_0.Add(intensity, likelihoods_0);
                likelihoodMap_1.Add(intensity, likelihoods_1);
            }

            likelihoodMap.Add(0, likelihoodMap_0);
            likelihoodMap.Add(1, likelihoodMap_1);
            return likelihoodMap;
        }


        public void UpdateModel(int response, double intensity) {
            /*
             * On every new trial k+1, find the new posterior by multiplying the old posterior P_k(s)
             * by the likelihood of generating `response` given some `intensity`
             * for all possible parameter sets
             * Then normalize by dividing by the integral (sum)
             */
            posterior = possible_posterior[response][intensity];
            intensityHistory.Add(intensity);
            responseHistory.Add(response);

        }

        public double NextStimulus() {

            // Compute the product of likelihood and current posterior array at each outcome and stimulus location.
            // P(s) * p(r|x,s)
            ResponseMap likexprior = likelihoodMap.ToDictionary(
                r => r.Key,
                r => r.Value.ToDictionary(
                    x => x.Key,
                    x => x.Value.ToDictionary(
                        p => p.Key,
                        p => p.Value * posterior[p.Key])));

            // Compute the total probability of each outcome at each stimulus location.
            // probability  p(r|x) = ∑_(s) P(s) * p(r|x,s)
            InputMap probability = likexprior.ToDictionary(
                r => r.Key,
                r => r.Value.ToDictionary(
                    x => x.Key,
                    x => x.Value.Sum(p => p.Value)));

            // Compute posterior PDF for each stimulus location and outcome.
            // P(s) = P(s) * p(r|x,s) | normalized
            // Just likexprior normalized
            possible_posterior = likexprior.ToDictionary(
                r => r.Key,
                r => r.Value.ToDictionary(
                    x => x.Key,
                    x => x.Value.ToDictionary(
                        p => p.Key,
                        p => p.Value / likexprior[r.Key][x.Key].Values.Sum())));


            // Compute the expected entropy for each stimulus location.
            // H(r,x) = - ∑ P(s)* log(P(s)
            InputMap H = possible_posterior.ToDictionary(
                r => r.Key,
                r => r.Value.ToDictionary(
                    x => x.Key,
                    x => x.Value.Where(p => !double.IsNegativeInfinity(Math.Log(p.Value)))
                                .Sum(p => -p.Value * Math.Log(p.Value)
                    )
                )
            );

            InputMap Hpk = H.ToDictionary(
                r => r.Key, // ResponseMap
                r => r.Value.ToDictionary(
                    x => x.Key, // IntensityMap
                    x => x.Value * probability[r.Key][x.Key]));  // P(r|x)H(r,x)


            var EHList = Hpk[0].Values.Zip(Hpk[1].Values, (x, y) => x + y).ToList(); // ∑_j P(r_j|x) * H(r_j,x)


            double min = EHList.Min();
            List<int> indices = new List<int>();


            for (int i = 0; i != EHList.Count(); ++i) {
                if (EHList[i] == min) {
                    indices.Add(i);
                }
            }

            int index = indices[rnd.Next(0, indices.Count())];
            return stimSamples[index];

        }

        public ParameterSet EstimateParams() {
            List<double> EHList = new List<double>();
            // Initialize parameter expected value list
            for (int i = 0; i != ParameterSet.size + 1; ++i) {
                EHList.Add(0);
            }

            foreach (var keyValuePair in posterior) {
                EHList = keyValuePair.Key.ExpectedValueAcc(keyValuePair.Value, EHList);
            }

            return new ParameterSet(EHList.ToArray());
        }

        static IEnumerable<IEnumerable<double>> CartesianProduct(IEnumerable<IEnumerable<double>> sequences) {
            IEnumerable<IEnumerable<double>> emptyProduct = new[] { Enumerable.Empty<double>() };
            /*
             * From https://ericlippert.com/2010/06/28/computing-a-cartesian-product-with-linq/
             */
            return sequences.Aggregate(emptyProduct, (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item }));
        }

        public static List<double> Range(double start, double stop, Nullable<double> _step = null) {
            if (start == stop) return new List<double> { start };
            double step = _step == null ? 1 : (double)_step;
            List<double> range = new List<double>();
            double curr = start;
            int steps = (int)((stop - start) / step);
            for (int i = 0; i != steps; i++) {
                range.Add(curr);
                curr += step;
            }

            return range;
        }

        public int Simulate(double intensity, ParameterSet parms) {
            double choice = PsychNormCdf(intensity, parms);
            if (rnd.NextDouble() < choice) {
                return 1;
            } else {
                return 0;
            }
        }



        public static void Main(string[] args) {

            List<int> means = new List<int> { 10, 20, 30, 40};
            List<int> sds = new List<int> { 2, 5 };
            //

            foreach (int mean in means) {
                foreach (int sd in sds) {
                    ParameterSet parameters = new ParameterSet(0.5, 0.02, mean, sd);
                    string path = @"/Users/zach/Documents/VU/Project/Caterina/QUEST Sim/" + mean+"_"+sd+".csv";

                    Console.WriteLine(path);
                    for (int j = 0; j != 30; ++j) {
                        QuestPlus qp = new QuestPlus(
                            0, 50, 1,
                            0.5, 0.5, 0,
                            0.02, 0.02, 0,
                            1, 50, 1,
                            1, 6, 1);
                        double inten = qp.NextStimulus();
                        for (int i = 0; i != 180; i++) {
                            int choice = qp.Simulate(inten, parameters);
                            qp.UpdateModel(choice, inten);
                            inten = qp.NextStimulus();
                        }

                        if (!File.Exists(path)) {
                            // Create a file to write to.
                            using (StreamWriter sw = File.CreateText(path)) {
                                sw.WriteLine(qp.EstimateParams());
                            }
                        } else {
                            using (StreamWriter sw = new StreamWriter(path, true)) {
                                sw.WriteLine(qp.EstimateParams());
                            }
                        }
                    }
                }
            }
            

        }


    }
}
