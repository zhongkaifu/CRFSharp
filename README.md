CRFSharp
========

CRFSharp is Conditional Random Fields implemented by .NET(C#), a machine learning algorithm for learning from labeled sequences of examples. It is widely used in Natural Language Process (NLP) tasks, for example: word breaker, postaging, named entity recognized and so on.

CRFSharp (aka CRF#) is based on .NET Framework 4.0 and its mainly algorithm is similar with CRF++ written by Taku Kudo. It encodes model parameters by L-BFGS. Moreover, it has many significant improvements than CRF++, such as totally parallel encoding, optimizing memory usage and so on. 

Compared with CRF++, CRFSharp is able to make full use of multi-core CPUs and use memory effectively for training, especially for very huge feature set. So in the same environment, CRFSharp is able to encode much more complex models with less cost than CRF++.

