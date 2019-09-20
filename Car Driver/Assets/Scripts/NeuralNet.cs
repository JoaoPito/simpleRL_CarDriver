using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeuralNet
{
    public class NN
    {
        float[] _hLayer;
        float[][] _hLayerW;
        float[] _hLayerB;

        float[] _outputW;
        float _outputB;

        float[] _fitness;
        float[] _inputs;

        public NN()
        {

        }

        public NN(int inputs,int hiddenLayer)
        {
            _inputs = new float[inputs];
            InitHiddenLayers(hiddenLayer);
        }

        //Inicia uma rede neural do zero ou com valores aleatorios entre -2 e 2
        public void InitHiddenLayers(int hLayer)
        {
            _hLayer = new float[hLayer];
            _hLayerB = new float[hLayer];
            _outputW = new float[hLayer];

            //_hLayerW[n hidden layer perceptrons][n inputs]
            _hLayerW = new float[hLayer][];

            _outputB = Random.Range(-5f, 5f);

            for (int i = 0;i < _hLayerW.GetLength(0); i++)
            {
                _hLayerW[i] = new float[_inputs.Length];

                _hLayer[i] = 0;
                _hLayerB[i] = Random.Range(-5f, 5f);
                _outputW[i] = Random.Range(-5f, 5f);

                for (int j = 0;j< _hLayerW[i].Length; j++)
                {
                    _hLayerW[i][j] = Random.Range(-5f, 5f);
                }
            }

            //Debug.Log("Random Brain Loaded! Weights: " + _hLayerW[0][0] + "/" + _hLayerW[0][1] + "/" + _hLayerW[1][0] + "/" + _hLayerW[1][1] + "len= " + _hLayerW.Length* _hLayerW[0].Length + " Biases: " + _hLayerB.Length + " Ow: " + _outputW + " Ob: " + _outputB);
        }
        
        //Inicia uma RN com valores predeterminados
        public void InitHiddenLayers(float[][] weights, float[] biases, float[] outputW, float outputB)
        {
            _hLayer = new float[biases.Length];
            _hLayerB = biases;

            _hLayerW = weights;

            _outputW = outputW;
            _outputB = outputB;

            for (int i = 0; i < _hLayer.Length; i++)
                _hLayer[i] = 0;

            //Debug.Log("Brain Loaded! Weights: " + weights.Length + weights[0].Length + " Biases: " + biases.Length + " Ow: " + outputW + " Ob: " + outputB);
        }
        
        //Inicia uma RN com uma string 
        public void InitHiddenLayers(string brain)
        {
            if (brain.Length < 10)
            {                
                return;
            }
            else
            {
                float[][] HLweights = new float[_hLayer.Length][];
                float[] HLbiases = new float[_hLayer.Length];
                float[] outputWeight = new float[_hLayer.Length];
                float outputBias = 0;

                string[] parsedBrain = brain.Split(';');

                outputBias = float.Parse(parsedBrain[parsedBrain.Length - 1]);

                outputWeight[3] = float.Parse(parsedBrain[parsedBrain.Length - 2]);
                outputWeight[2] = float.Parse(parsedBrain[parsedBrain.Length - 3]);
                outputWeight[1] = float.Parse(parsedBrain[parsedBrain.Length - 4]);
                outputWeight[0] = float.Parse(parsedBrain[parsedBrain.Length - 5]);

                HLbiases[3] = float.Parse(parsedBrain[parsedBrain.Length - 6]);
                HLbiases[2] = float.Parse(parsedBrain[parsedBrain.Length - 7]);
                HLbiases[1] = float.Parse(parsedBrain[parsedBrain.Length - 8]);
                HLbiases[0] = float.Parse(parsedBrain[parsedBrain.Length - 9]);

                int a = 0;

                for (int i = 0; i < HLweights.Length; i++)
                {
                    HLweights[i] = new float[_inputs.Length];

                    for (int j = 0; j < HLweights[i].Length; j++)
                    {
                        HLweights[i][j] = float.Parse(parsedBrain[a]);

                        a++;
                    }
                }

                //Debug.LogWarning("InitHiddenLayers: brain: " + brain + " bias loaded: " + HLbiases[0] + "," + HLbiases[1] + " BrainArrayBias: " + parsedBrain[parsedBrain.Length - 6] + "," + parsedBrain[parsedBrain.Length - 5] + " Last HLWeight: " + HLweights[3][4]);
                
                InitHiddenLayers(HLweights, HLbiases, outputWeight, outputBias);
            }
        }

            //Calcula a Rede Neural por inteiro e retorna o output
        public float CalculateNN(float[] inputs)
        {
            float ret = 0;

            ret = CalculateOutput(CalculateHL(inputs, _hLayer.Length));

            return ret;
        }

        //Calcula a Hidden Layer da RN 
        float[] CalculateHL(float[] input, int HLLength)
        {
            float[] ret = new float[HLLength];
            float o = 0;

            //calcula cada perceptron na hidden layer
            for(int i = 0; i < ret.Length; i++)
            {
                for (int j = 0; j < input.Length; j++)
                {
                    //calcula os weights;i = n hidden layer;j = n inputs
                    o += input[j] * _hLayerW[i][j];
                   // Debug.Log("HL: W= " + _hLayerW[i][j] + " * In= " + input[j] + " Calculated: " + o + " ij= " + i + j);
                }
                ret[i] = ReLU(o + _hLayerB[i]);
                //Debug.Log("Calculated ReLU: " + ret[i] + " i: " + i);
            }
            

            return ret;
        }

        //Calcula o output baseado na camada anterior, basicamente calcula do mesmo jeito que nas outras camadas
        float CalculateOutput(float[] hiddenLayer)
        {
            float ret = 0;
            float ret1 = 0;
            float o = 0;
            
            for(int i = 0; i < hiddenLayer.Length; i++)
            {
                o += hiddenLayer[i] * _outputW[i];
            }

            ret1 = o + _outputB;
           // Debug.Log("Calculated ReLU: " + ret1);

            ret = ret1 / (1 + Mathf.Abs(ret1));//Faz o output ser um valor entre -1 e 1
            //Debug.Log("ret: " + ret);

            return ret;
        }

        public float ReLU(float inp)
        {
            float ret = Mathf.Max(0, inp);
            return ret;
        }

        public string ExtractBrain()
        {
            string brain = "";
            int brainSize = _hLayerW.Length + _hLayerB.Length +_outputW.Length + 1;
            Debug.LogWarning(brainSize);

            int a = 0;

            for(int i =0; i < brainSize; i++)
            {
                if (i < _hLayerW.Length)//Iterates over the hl weights
                {
                    for (int j = 0; j < _hLayerW[i].Length; j++)
                    {
                         brain += _hLayerW[i][j].ToString() + ";";
                    }
                }
                if(i >= _hLayerW.Length && i < _hLayerW.Length + _hLayerB.Length)//Iterates over hl biases
                {
                    brain += _hLayerB[i - _hLayerW.Length].ToString() + ";";
                }
                if(i >= _hLayerW.Length + _hLayerB.Length && i < _hLayerW.Length + _hLayerB.Length+ _outputW.Length)//Iterates over output Weights
                {
                    brain += _outputW[i - _hLayerW.Length - _hLayerB.Length].ToString() + ";";
                }
                if(i >= _hLayerW.Length + _hLayerB.Length + _outputW.Length)//Iterates over output bias
                {
                    brain += _outputB.ToString();
                }
            }

            Debug.LogWarning("Extract Brain: ret: " + brain + "size: " + brain.ToCharArray().Length);
            return brain;
        }

        public (float[][],float[],float[],float) ExtractBrainFloat()
        {
            return (_hLayerW,_hLayerB,_outputW,_outputB);
        }
    }
}
