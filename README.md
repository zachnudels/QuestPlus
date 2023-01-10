# QuestPlus

With the intention of being used for psychophysical experiments using Unity: A C# implementation of the Quest+ staircasing algorithm defined by Watson (2017) in QUEST+: A general multidimensional Bayesian adaptive psychometric method. The algorithm estimates the psychophysical parameters online during an experiment while simultaneously providing the stimulus on each trial to best maximize information about these parameters. Be aware of the strong independence assumptions made by this algorithm!

## Usage 
For any parametric function (usually psychometric), the `QuestPlus` class will estimate the parameters to best fit that function to a given participant's reponses during an experiment. Use the `ParameterSet` class to define the parameters used by the function. Then during your experiment, iteratively call `stimulus = NextStimulus()` to produce the stimulus and once the participant has made their response, call `UpdateModel(response, stimulus)` to update the estimated parameters. You can continue for a certain number of trials or until some convergence criterion is met. 

The class also contains a `Simulate()` method that assumes a Normally distributed response function which can be used to test the algorithm. Such a simulation is implemented in the `Main()` method of the class.
