using System;
using System.Collections.Generic;
namespace test {
    public class ParameterSet {

        public double guess;
        public double lapse;
        public double mean;
        public double sd;
        public static int size = 4;

        public ParameterSet(double guess, double lapse, double mean, double sd) {
            this.guess = guess;
            this.lapse = lapse;
            this.mean = mean;
            this.sd = sd;

        }

        public ParameterSet(double[] args) {
            this.guess = args[0];
            this.lapse = args[1];
            this.mean = args[2];
            this.sd = args[3];
        }

        public override bool Equals(Object obj) {
            if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            }
            ParameterSet other = (ParameterSet)obj;
                return this.guess == other.guess
                    && this.lapse == other.lapse
                    && this.mean == other.mean
                    && this.sd == other.sd;
        }

        public override int GetHashCode() {
            int hash = 17;

            hash = hash * 23 + guess.GetHashCode();
            hash = hash * 23 + lapse.GetHashCode();
            hash = hash * 23 + mean.GetHashCode();
            hash = hash * 23 + sd.GetHashCode();

            return hash;
        }

        public override string ToString() {
            return guess + ", " +
                lapse + ", " +
                mean + ", " +
                sd;
        }

        public List<double> ExpectedValueAcc(double probability, List<double> EHList) {

            EHList[0] += guess * probability;
            EHList[1] += lapse * probability;
            EHList[2] += mean * probability;
            EHList[3] += sd * probability;

            return EHList;
        }
    }
}
