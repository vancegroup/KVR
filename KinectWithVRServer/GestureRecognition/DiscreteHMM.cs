using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectWithVRServer
{
    class DiscreteHMM<T, U>
    {
        private U[] states;
        private double[,] stateTransitionProbabilities;
        private double[] initialStateDistribution;

        //Discrete specific variables
        private double[,] symbolDistributionDiscrete;
        private T[] symbols;


        /// <summary>
        /// This constructor initializes a discrete hidden Markov model.
        /// </summary>
        /// <param name="symbolsList">The list of possible symbols in the HMM.</param>
        /// <param name="statesList">The list of possible states in the HMM.</param>
        /// <param name="stateTransitions"></param>
        /// <param name="initialDistribution"></param>
        /// <param name="symbolDistributions"></param>
        public DiscreteHMM(T[] symbolsList, U[] statesList, double[,] stateTransitions, double[] initialDistribution, double[,] symbolDistributions)
        {
            //TODO: Make the parameters other than symbols and states optional and have the others be autoset to a default (which is then trained later)

            #region Check the symbols list validity
            if (symbolsList != null)
            {
                for (int i = 0; i < symbolsList.Length; i++)
                {
                    if (symbolsList[i] == null)
                    {
                        throw new ArgumentException("Null symbols are not supported in the symbols list.");
                    }
                    //TODO: Check if all the symbols are unique
                }
            }
            else
            {
                throw new ArgumentNullException("symbolsList");
            }
            #endregion

            #region Check the states list validity
            if (statesList != null)
            {
                for (int i = 0; i < statesList.Length; i++)
                {
                    if (statesList[i] == null)
                    {
                        throw new ArgumentException("Null states are not supported in the states list.");
                    }
                    //TODO: Check if all the states are unique
                }
            }
            else
            {
                throw new ArgumentNullException("statesList");
            }
            #endregion

            #region Check the validity of state transitions
            if (stateTransitions != null)
            {
                //Any state must have a probability to go to any other state, including itself.  This probability can be zero to create non-ergodic models
                //This condition requires a square matrix
                if (stateTransitions.GetLength(0) == stateTransitions.GetLength(1) && stateTransitions.GetLength(0) == statesList.Length)
                {
                    //Check if all the probabilities are valid 
                    int arraySize = stateTransitions.GetLength(0);
                    bool stateOK = true;
                    for (int i = 0; i < arraySize; i++)
                    {
                        for (int j = 0; j < arraySize; j++)
                        {
                            if (stateTransitions[i, j] < 0 || stateTransitions[i, j] > 1.01)
                            {
                                stateOK = false;
                            }
                        }
                    }
                    if (stateOK)
                    {
                        //Check if all the probabilities out of a state sum to 1 (ish)
                        double sum = 0.0;
                        for (int y = 0; y < arraySize; y++)
                        {
                            for (int x = 0; x < arraySize; x++)
                            {
                                sum += stateTransitions[y, x];
                            }

                            if (Math.Abs(1.0 - sum) > 0.01)
                            {
                                throw new ArgumentException("All the transition probabilities out of one state must sum to one.");
                            }
                            sum = 0.0;
                        }
                    }
                    else
                    {
                        throw new ArgumentException("All the state transition probabilities must be in the range [0, 1].");
                    }
                }
                else
                {
                    throw new ArgumentException("The state transition matrix must be square, with each side equal to the number of possible states.");
                }
            }
            else
            {
                throw new ArgumentNullException("stateTransitions");
            }
            #endregion

            #region Check the symbol distribution
            if (symbolDistributions != null)
            {
                if (symbolDistributions.GetLength(1) == symbolsList.Length && symbolDistributions.GetLength(0) == statesList.Length)
                {
                    //Check if all the probabilities are valid 
                    int arraySizeX = symbolDistributions.GetLength(1);
                    int arraySizeY = symbolDistributions.GetLength(0);
                    bool probabilityOK = true;
                    for (int y = 0; y < arraySizeY; y++)
                    {
                        for (int x = 0; x < arraySizeX; x++)
                        {
                            if (symbolDistributions[y, x] < 0 || symbolDistributions[y, x] > 1)
                            {
                                probabilityOK = false;
                            }
                        }
                    }
                    if (probabilityOK)
                    {
                        //Check if all the probabilities out of a state sum to 1 (ish)
                        double sum = 0.0;
                        for (int y = 0; y < arraySizeY; y++)
                        {
                            for (int x = 0; x < arraySizeX; x++)
                            {
                                sum += symbolDistributions[y, x];
                            }

                            if (Math.Abs(1.0 - sum) > 0.01)
                            {
                                throw new ArgumentException("All the symbol distribution probabilities for one state must sum to one.");
                            }
                            sum = 0.0;
                        }
                    }
                    else
                    {
                        throw new ArgumentException("All the symbol distribution probabilities must be in the range [0, 1].");
                    }
                }
                else
                {
                    throw new ArgumentException("The symbol distribution matrix must have a width equal to the number of symbols and a height equal to the number of states.");
                }
            }
            else
            {
                throw new ArgumentNullException("symbolDistributions");
            }
            #endregion

            #region Check the validity of the initial distribution
            if (initialDistribution != null)
            {
                if (initialDistribution.Length == statesList.Length)
                {
                    bool probabilityOK = true;
                    double sum = 0.0;
                    for (int i = 0; i < initialDistribution.Length; i++)
                    {
                        if (initialDistribution[i] < 0 || initialDistribution[i] > 1.01)
                        {
                            probabilityOK = false;
                        }
                        sum += initialDistribution[i];
                    }
                    if (probabilityOK)
                    {
                        if (Math.Abs(1.0 - sum) > 0.01)
                        {
                            throw new ArgumentException("All the symbol distribution probabilities for one state must sum to one.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("All the initial distribution probabilities must be in the range [0, 1].");
                    }
                }
                else
                {
                    throw new ArgumentException("The initial distribution array and the state array must be the same size.");
                }
            }
            else
            {
                throw new ArgumentNullException("initialDistribution");
            }
            #endregion

            //Now that everything has been checked to be valid, copy over the data
            symbols = symbolsList;
            states = statesList;
            stateTransitionProbabilities = stateTransitions;
            initialStateDistribution = initialDistribution;
            symbolDistributionDiscrete = symbolDistributions;
            //symbolType = symbolT;
            //stateType = stateT;
        }

        public DiscreteHMM(HMMModel model, T[] symbolsList, U[] statesList)
        {
            #region Check the symbols list validity
            if (symbolsList != null)
            {
                for (int i = 0; i < symbolsList.Length; i++)
                {
                    if (symbolsList[i] == null)
                    {
                        throw new ArgumentException("Null symbols are not supported in the symbols list.");
                    }
                    //TODO: Check if all the symbols are unique
                }
            }
            else
            {
                throw new ArgumentNullException("symbolsList");
            }
            #endregion

            #region Check the states list validity
            if (statesList != null)
            {
                for (int i = 0; i < statesList.Length; i++)
                {
                    if (statesList[i] == null)
                    {
                        throw new ArgumentException("Null states are not supported in the states list.");
                    }
                    //TODO: Check if all the states are unique
                }
            }
            else
            {
                throw new ArgumentNullException("statesList");
            }
            #endregion

            symbols = symbolsList;
            states = statesList;

            #region Create the probability matrices
            stateTransitionProbabilities = new double[statesList.Length, statesList.Length];
            initialStateDistribution = new double[statesList.Length];
            symbolDistributionDiscrete = new double[statesList.Length, symbolsList.Length];
            double evenSymProb = 1.0 / (double)symbolsList.Length;

            if (model == HMMModel.Ergodic)
            {
                double evenStateProb = 1.0 / (double)statesList.Length;

                //Setup the initial state distribution
                for (int i = 0; i < statesList.Length; i++)
                {
                    initialStateDistribution[i] = evenStateProb;
                }

                //Setup the state transition probabilities
                for (int i = 0; i < statesList.Length; i++)
                {
                    for (int j = 0; j < statesList.Length; j++)
                    {
                        stateTransitionProbabilities[i, j] = evenStateProb;
                    }
                }

                //Setup the symbol distribution
                for (int i = 0; i < statesList.Length; i++)
                {
                    for (int j = 0; j < symbolsList.Length; j++)
                    {
                        symbolDistributionDiscrete[i, j] = evenSymProb;
                    }
                }
            }
            else if (model == HMMModel.LeftToRight)
            {
                //Setup the initial state distribution
                for (int i = 0; i < statesList.Length; i++)
                {
                    if (i == 0)
                    {
                        initialStateDistribution[i] = 1.0;
                    }
                    else
                    {
                        initialStateDistribution[i] = 0;
                    }
                }

                //Setup the state transition probabilities
                for (int i = 0; i < statesList.Length; i++)
                {
                    for (int j = 0; j < statesList.Length; j++)
                    {
                        if (j == i || j == i + 1)
                        {
                            stateTransitionProbabilities[i, j] = 0.5;
                        }
                        else
                        {
                            stateTransitionProbabilities[i, j] = 0.0;
                        }
                    }
                }

                //Setup the symbol distribution
                for (int i = 0; i < statesList.Length; i++)
                {
                    for (int j = 0; j < symbolsList.Length; j++)
                    {
                        symbolDistributionDiscrete[i, j] = evenSymProb;
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// This function calculates the probability of a observed sequence of symbols using the forward-backward procedure.
        /// Note: This function may have underflow issues.  Use the log version if possible.
        /// </summary>
        /// <param name="observedSymbolSequence">The observed symbol sequence.</param>
        /// <returns>The probability of the occurance of the observed sequence.</returns>
        internal double FastObservationSequenceProbability(T[] observedSymbolSequence)
        {
            double[,] alpha = ForwardVariables(observedSymbolSequence);

            //Termination step
            double finalProb = 0.0;
            for (int i = 0; i < states.Length; i++)
            {
                finalProb += alpha[observedSymbolSequence.Length - 1, i];
            }

            return finalProb;
        }

        /// <summary>
        /// This function calculates the log of the probability of a observed sequence of symbols using the fordward-backward procedure.
        /// This function has better numerical stability than the unmodified verison, but returns log(P) instead of P.
        /// </summary>
        /// <param name="observedSymbolSequence">The observed symbol sequence.</param>
        /// <returns>The probability of the occurance of the observed sequence.</returns>
        internal double LogFastObservationSequenceProbability(T[] observedSymbolSequence)
        {
            //Initialize the alphas
            double[,] alpha = new double[observedSymbolSequence.Length, states.Length];
            double[] alphaSum = new double[observedSymbolSequence.Length];
            for (int i = 0; i < states.Length; i++)
            {
                alpha[0, i] = initialStateDistribution[i] * SymbolProbability(observedSymbolSequence[0], states[i]);
                alphaSum[0] += alpha[0, i];
            }
            //Scale alpha0 to alpha[hat]0
            for (int i = 0; i < states.Length; i++)
            {
                alpha[0, i] = alpha[0, i] / alphaSum[0];
            }

            //Induction step
            for (int t = 1; t < observedSymbolSequence.Length; t++)
            {
                for (int j = 0; j < states.Length; j++)
                {
                    double temp = 0.0;
                    for (int i = 0; i < states.Length; i++)
                    {
                        temp += alpha[t - 1, i] * stateTransitionProbabilities[i, j];
                    }
                    temp *= SymbolProbability(observedSymbolSequence[t], states[j]);

                    alpha[t, j] = temp;
                    alphaSum[t] += temp;
                }

                //Scale alpha to alpha[hat]
                for (int i = 0; i < states.Length; i++)
                {
                    alpha[t, i] = alpha[t, i] * (1.0 / alphaSum[t]);
                }
            }

            //Scaled termination step
            double finalProb = 0.0;
            for (int t = 0; t < observedSymbolSequence.Length; t++)
            {
                finalProb += Math.Log(1.0 / alphaSum[t]);
            }
            finalProb *= -1.0;

            return finalProb;
        }

        /// <summary>
        /// This calculates the most likely sequence of states based on an observed sequence or symbols using the Viterbi Algorithm
        /// </summary>
        /// <param name="observedSymbolSequence">The observed symbol sequence.</param>
        /// <returns></returns>
        internal U[] BestStateSequence(T[] observedSymbolSequence)
        {
            U[] optimizedSequence = new U[observedSymbolSequence.Length];

            //Initialization step
            double[,] smallDelta = new double[observedSymbolSequence.Length, states.Length];
            int[,] psi = new int[observedSymbolSequence.Length, states.Length];
            for (int i = 0; i < states.Length; i++)
            {
                smallDelta[0, i] = initialStateDistribution[i] * SymbolProbability(observedSymbolSequence[0], states[i]);
                psi[0, i] = 0;
            }

            //Recursion step (I don't see the recursion in this, but Rabiner calls this the recursion step, so I will follow his lead...)
            for (int t = 1; t < observedSymbolSequence.Length; t++)
            {
                for (int j = 0; j < states.Length; j++)
                {
                    double max = double.MinValue;
                    int argmax = -1;
                    for (int i = 0; i < states.Length; i++)
                    {
                        double temp = smallDelta[t - 1, i] * stateTransitionProbabilities[i, j];
                        if (temp > max)
                        {
                            max = temp;
                            argmax = i;
                        }
                    }
                    smallDelta[t, j] = max * SymbolProbability(observedSymbolSequence[t], states[j]);
                    psi[t, j] = argmax;
                }
            }

            //Termination step
            double Pstar = double.MinValue;
            int qstar = int.MinValue;
            for (int i = 0; i < states.Length; i++)
            {
                if (Pstar < smallDelta[observedSymbolSequence.Length - 1, i])
                {
                    Pstar = smallDelta[observedSymbolSequence.Length - 1, i];
                    qstar = i;
                }
            }
            optimizedSequence[observedSymbolSequence.Length - 1] = states[qstar];

            //Path backtracking
            for (int t = observedSymbolSequence.Length - 2; t >= 0; t--)
            {
                int tempLocale = psi[t + 1, qstar];
                qstar = tempLocale;
                optimizedSequence[t] = states[tempLocale];
            }


            return optimizedSequence;
        }

        internal void TrainHMM(T[] trainingData)
        {
            bool iterating = true;
            double[] tempPI = new double[initialStateDistribution.Length];
            double[,] tempA = new double[stateTransitionProbabilities.GetLength(0), stateTransitionProbabilities.GetLength(1)];
            double[,] tempB = new double[symbolDistributionDiscrete.GetLength(0), symbolDistributionDiscrete.GetLength(1)];
            double[,] alpha;
            double[,] beta;
            double[, ,] xi = new double[trainingData.Length - 1, states.Length, states.Length];
            double[,] gamma = new double[trainingData.Length, states.Length];
            DiscreteHMM<T, U> newHMM;

            while (iterating)
            {
                alpha = ForwardVariables(trainingData);
                beta = BackwardVariables(trainingData);
                double totalProb = FastObservationSequenceProbability(trainingData);

                //Compute xi and gamma
                for (int t = 0; t < trainingData.Length; t++)
                {
                    for (int i = 0; i < states.Length; i++)
                    {
                        if (t < trainingData.Length - 1)
                        {
                            for (int j = 0; j < states.Length; j++)
                            {
                                xi[t, i, j] = (alpha[t, i] * stateTransitionProbabilities[i, j] * SymbolProbability(trainingData[t + 1], states[j]) * beta[t + 1, j]) / totalProb;
                            }
                        }
                        gamma[t, i] = (alpha[t, i] * beta[t, i]) / totalProb;
                    }
                }

                for (int i = 0; i < states.Length; i++)
                {
                    tempPI[i] = gamma[0, i];

                    for (int j = 0; j < states.Length; j++)
                    {
                        double aNum = 0;
                        double aDen = 0;
                        for (int t = 0; t < trainingData.Length - 1; t++)
                        {
                            aNum += xi[t, i, j];
                            aDen += gamma[t, i];
                        }
                        tempA[i, j] = aNum / aDen;
                    }
                }

                for (int j = 0; j < states.Length; j++)
                {
                    for (int k = 0; k < symbols.Length; k++)
                    {
                        double bNum = 0.0;
                        double bDen = 0.0;

                        for (int t = 0; t < trainingData.Length; t++)
                        {
                            if (trainingData[t].Equals(symbols[k]))
                            {
                                bNum += gamma[t, j];
                            }
                            bDen += gamma[t, j];
                        }

                        tempB[j, k] = bNum / bDen;
                    }
                }


                newHMM = new DiscreteHMM<T, U>(symbols, states, tempA, tempPI, tempB);
                if (newHMM.LogFastObservationSequenceProbability(trainingData) > LogFastObservationSequenceProbability(trainingData))
                {
                    Array.Copy(tempPI, initialStateDistribution, tempPI.Length);
                    Array.Copy(tempA, stateTransitionProbabilities, tempA.Length);
                    Array.Copy(tempB, symbolDistributionDiscrete, tempB.Length);
                }
                else
                {
                    iterating = false;
                }
            }
        }

        internal void TrainHMMScaled(List<T[]> trainingData)
        {
            bool iterating = true;

            while (iterating)
            {
                double[] tempPI = new double[initialStateDistribution.Length];
                double[,] tempA = new double[stateTransitionProbabilities.GetLength(0), stateTransitionProbabilities.GetLength(1)];
                double[,] tempANum = new double[stateTransitionProbabilities.GetLength(0), stateTransitionProbabilities.GetLength(1)];
                //double[,] tempADen = new double[stateTransitionProbabilities.GetLength(0), stateTransitionProbabilities.GetLength(1)];
                double[] tempADen = new double[stateTransitionProbabilities.GetLength(0)];
                double[,] tempB = new double[symbolDistributionDiscrete.GetLength(0), symbolDistributionDiscrete.GetLength(1)];
                double[,] tempBNum = new double[symbolDistributionDiscrete.GetLength(0), symbolDistributionDiscrete.GetLength(1)];
                double[,] tempBDen = new double[symbolDistributionDiscrete.GetLength(0), symbolDistributionDiscrete.GetLength(1)];
                double[,] alpha;
                double[,] beta;
                double[] c;
                DiscreteHMM<T, U> newHMM;

                for (int k = 0; k < trainingData.Count; k++)
                {
                    double[, ,] xi = new double[trainingData[k].Length - 1, states.Length, states.Length];
                    double[,] gamma = new double[trainingData[k].Length, states.Length];
                    ForwardAndBackwardVariablesScaled(trainingData[k], out alpha, out beta, out c);
                    double totalProb = FastObservationSequenceProbability(trainingData[k]);


                    //Compute xi and gamma
                    for (int t = 0; t < trainingData[k].Length; t++)
                    {
                        for (int i = 0; i < states.Length; i++)
                        {
                            if (t < trainingData[k].Length - 1)
                            {
                                for (int j = 0; j < states.Length; j++)
                                {
                                    xi[t, i, j] = (alpha[t, i] * stateTransitionProbabilities[i, j] * SymbolProbability(trainingData[k][t + 1], states[j]) * beta[t + 1, j]);
                                }
                            }
                            gamma[t, i] = (alpha[t, i] * beta[t, i]) / c[t];
                        }
                    }

                    for (int i = 0; i < states.Length; i++)
                    {
                        tempPI[i] += gamma[0, i];

                        for (int j = 0; j < states.Length; j++)
                        {
                            double aNum = 0;

                            for (int t = 0; t < trainingData[k].Length - 1; t++)
                            {
                                aNum += xi[t, i, j];
                            }
                            tempANum[i, j] += aNum;
                        }

                        double aDen = 0;
                        for (int t = 0; t < trainingData[k].Length - 1; t++)
                        {
                            aDen += gamma[t, i];
                        }
                        tempADen[i] += aDen;
                    }

                    for (int j = 0; j < states.Length; j++)
                    {
                        for (int v = 0; v < symbols.Length; v++)
                        {
                            double bNum = 0.0;
                            double bDen = 0.0;

                            for (int t = 0; t < trainingData[k].Length; t++)
                            {
                                if (trainingData[k][t].Equals(symbols[v]))
                                {
                                    bNum += gamma[t, j];
                                }
                                bDen += gamma[t, j];
                            }

                            tempBNum[j, v] += bNum;
                            tempBDen[j, v] += bDen;
                        }
                    }
                }

                //Calculate the final reestimated values
                for (int i = 0; i < states.Length; i++)
                {
                    tempPI[i] = tempPI[i] / (double)trainingData.Count; //this isn't in Rabiner's tutorial; however, I think it is needed to reestimate ergodic models from multiple sequences

                    for (int j = 0; j < states.Length; j++)
                    {
                        tempA[i, j] = tempANum[i, j] / tempADen[i];
                    }

                    for (int v = 0; v < symbols.Length; v++)
                    {
                        tempB[i, v] = tempBNum[i, v] / tempBDen[i, v];
                    }
                }

                newHMM = new DiscreteHMM<T, U>(symbols, states, tempA, tempPI, tempB);
                double pOld = 0.0;
                double pNew = 0.0;
                for (int k = 0; k < trainingData.Count; k++)
                {
                    pOld += LogFastObservationSequenceProbability(trainingData[k]);         //If you want to multiply variables, you add the logs... DUH!
                    pNew += newHMM.LogFastObservationSequenceProbability(trainingData[k]);
                }

                if (pNew > pOld)
                {
                    Array.Copy(tempPI, initialStateDistribution, tempPI.Length);
                    Array.Copy(tempA, stateTransitionProbabilities, tempA.Length);
                    Array.Copy(tempB, symbolDistributionDiscrete, tempB.Length);
                }
                else
                {
                    iterating = false;
                }
            }
        }

        private double[,] ForwardVariables(T[] observedSymbolSequence)
        {
            //Initialize the alphas
            double[,] alpha = new double[observedSymbolSequence.Length, states.Length];
            for (int i = 0; i < states.Length; i++)
            {
                alpha[0, i] = initialStateDistribution[i] * SymbolProbability(observedSymbolSequence[0], states[i]);
            }

            //Induction step
            for (int t = 1; t < observedSymbolSequence.Length; t++)
            {
                for (int j = 0; j < states.Length; j++)
                {
                    double temp = 0.0;
                    for (int i = 0; i < states.Length; i++)
                    {
                        temp += alpha[t - 1, i] * stateTransitionProbabilities[i, j];
                    }
                    temp *= SymbolProbability(observedSymbolSequence[t], states[j]);

                    alpha[t, j] = temp;
                }
            }

            return alpha;
        }

        private double[,] BackwardVariables(T[] observedSymbolSequence)
        {
            int bigT = observedSymbolSequence.Length;
            int N = states.Length;
            double[,] beta = new double[observedSymbolSequence.Length, states.Length];

            //Initialization
            for (int i = 0; i < states.Length; i++)
            {
                beta[bigT - 1, i] = 1.0;
            }

            //Induction
            for (int t = bigT - 2; t >= 0; t--)
            {
                for (int i = 0; i < N; i++)
                {
                    double temp = 0.0;
                    for (int j = 0; j < N; j++)
                    {
                        temp += stateTransitionProbabilities[i, j] * SymbolProbability(observedSymbolSequence[t + 1], states[j]) * beta[t + 1, j];
                    }

                    beta[t, i] = temp;
                }
            }

            return beta;
        }

        private double[,] ForwardVariablesScaled(T[] observedSymbolSequence, out double[] scalers)
        {
            //Initialize the alphas
            double[,] alpha = new double[observedSymbolSequence.Length, states.Length];
            double[] alphaSum = new double[observedSymbolSequence.Length];
            scalers = new double[observedSymbolSequence.Length];

            for (int i = 0; i < states.Length; i++)
            {
                alpha[0, i] = initialStateDistribution[i] * SymbolProbability(observedSymbolSequence[0], states[i]);
                alphaSum[0] += alpha[0, i];
            }
            scalers[0] = 1.0 / alphaSum[0];
            //Scale alpha0 to alpha[hat]0
            for (int i = 0; i < states.Length; i++)
            {
                alpha[0, i] = alpha[0, i] / alphaSum[0];
            }

            //Induction step
            for (int t = 1; t < observedSymbolSequence.Length; t++)
            {
                for (int j = 0; j < states.Length; j++)
                {
                    double temp = 0.0;
                    for (int i = 0; i < states.Length; i++)
                    {
                        temp += alpha[t - 1, i] * stateTransitionProbabilities[i, j];
                    }
                    temp *= SymbolProbability(observedSymbolSequence[t], states[j]);

                    alpha[t, j] = temp;
                    alphaSum[t] += temp;
                }

                //Scale alpha to alpha[hat]
                for (int i = 0; i < states.Length; i++)
                {
                    alpha[t, i] = alpha[t, i] * (1.0 / alphaSum[t]);
                }

                scalers[t] = 1.0 / alphaSum[t];
            }

            return alpha;
        }

        private void ForwardAndBackwardVariablesScaled(T[] observedSymbolSequence, out double[,] forward, out double[,] backwards, out double[] scalers)
        {
            forward = ForwardVariablesScaled(observedSymbolSequence, out scalers);

            int bigT = observedSymbolSequence.Length;
            int N = states.Length;
            double[,] beta = new double[observedSymbolSequence.Length, states.Length];

            //Initialization
            for (int i = 0; i < states.Length; i++)
            {
                beta[bigT - 1, i] = 1.0 * scalers[bigT - 1];
            }

            //Induction
            for (int t = bigT - 2; t >= 0; t--)
            {
                for (int i = 0; i < N; i++)
                {
                    double temp = 0.0;
                    for (int j = 0; j < N; j++)
                    {
                        temp += stateTransitionProbabilities[i, j] * SymbolProbability(observedSymbolSequence[t + 1], states[j]) * beta[t + 1, j];
                    }

                    beta[t, i] = temp * scalers[t];
                }
            }

            backwards = beta;
        }

        public double SymbolProbability(T symbol, U state)
        {
            double prob = 0.0;

            prob = symbolDistributionDiscrete[GetStateIndex(state), GetSymbolIndex(symbol)];

            return prob;
        }

        private int GetSymbolIndex(T symbol)
        {
            for (int i = 0; i < symbols.Length; i++)
            {
                if (symbols[i].Equals(symbol))
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetStateIndex(U state)
        {
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].Equals(state))
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public enum HMMModel { LeftToRight, Ergodic }
}
