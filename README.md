CRFSharp
========
CRFSharp is Conditional Random Fields (CRF) implemented by .NET(C#), a machine learning algorithm for learning from labeled sequences of examples.

## Overview
CRFSharp is Conditional Random Fields implemented by .NET(C#), a machine learning algorithm for learning from labeled sequences of examples. It is widely used in Natural Language Process (NLP) tasks, for example: word breaker, postaging, named entity recognized and so on.

 CRFSharp (aka CRF#) is based on .NET Framework 4.0 and its mainly algorithm is similar with CRF++ written by Taku Kudo. It encodes model parameters by L-BFGS. Moreover, it has many significant improvements than CRF++, such as totally parallel encoding, optimizing memory usage and so on. 

 Currently, when training corpus, compared with CRF++, CRFSharp can make full use of multi-core CPUs and use memory effectively, especially for very huge size training corpus and tags. So in the same environment, CRFSharp is able to encode much more complex models with less cost than CRF++.

The following screenshot is an example that CRFSharp is running on a machine with 16 cores CPUs and 96GB memory.
![](http://download-codeplex.sec.s-msft.com/Download?ProjectName=crfsharp&DownloadId=600636)
The training corpus has 1.24 million records with nearly 1.2 billion features. From the screenshot, all CPU cores are full used and memory usage is stable. The average encoding time per iteration is 3 minutes and 33 seconds.

Besides command line tool, CRFSharp has also provided APIs and these APIs can be used into other projects and services for key techincal tasks. For example: WordSegment project has used CRFSharp to recognize named entity; Query Term Analyzer project has used it to analyze query term important level in word formation and Geography Coder project has used it to detect geo-entity from text. For detailed information about APIs, please see section [Use CRFSharp API in your project] in below.

 To use CRFSharp, we need to prepare corpus and design feature templates at first. CRFSharp's file formats are compatible with CRF++(official website:http://crfpp.googlecode.com/svn/trunk/doc/index.html). The following paragraphs will introduce data formats and how to use CRFSharp in both command line and APIs

## Training file format
Training corpus contains many records to describe what the model should be. For each record, it is split into one or many tokens and each token has one or many dimension features to describe itself. 

 In training file, each record can be represented as a matrix and ends with an empty line. In the matrix, each row describes one token and its features, and each column represents a feature in one dimension. In entire training corpus, the number of column must be fixed.

 When CRFSharp encodes, if the column size is N, according template file describes, the first N-1 columns will usually be used as input data to generate binary feature set and train model. The Nth column (aka last column) is the answer that the model should output. The means, for one record, if we have an ideal encoded model, given all tokens’ the first N-1 columns, the model should output each token’s Nth column data as the entire record’s answer.

 There is an example (a bigger training example file is at download section, you can see and download it there):  

Word       | Pos  | Tag
-----------|------|----
!          | PUN  | S
Tokyo      | NNP  | S_LOCATION
and        | CC   | S
New        | NNP  | B_LOCATION
York       | NNP  | E_LOCATION
are        | VBP  | S
major      | JJ   | S
financial  | JJ   | S
centers    | NNS  | S
.          | PUN  | S
           |      |    
!          | PUN  | S
p          | FW   | S
'          | PUN  | S
y          | NN   | S
h          | FW   | S
44         | CD   | S
University | NNP  | B_ORGANIZATION
of         | IN   | M_ORGANIZATION
Texas      | NNP  | M_ORGANIZATION
Austin     | NNP  | E_ORGANIZATION

The example is for labeling named entities in records. It has two records and each token has three columns. The first column is the term of a token, the second column is the token’s pos-tag result and the third column is to describe whether the token is a named entity or a part of named entity and its type. The first and the second columns are input data for encoding model, and the third column is the model ideal output as answer.

In above example, we designed output answer as "POS_TYPE". POS means the position of the term in the chunk or named entity, TYPE means the output type of the term. 

For POS, it supports four types as follows: 
S: the chunk has only one term 
B: the begin term of the chunk 
M: one of the middle term in the chunk 
E: the end term of the chunk 

For TYPE, the example contains many types as follows: 
 ORGANIZATION : the name of one organization 
 LOCATION : the name of one location 
 For output answer without TYPE, it's just a normal term, not a named entity. 

## Test file format
Test file has the similar format as training file. The only different between training and test file is the last column. In test file, all columns are features for CRF model.

## CRFSharp command line tools
CRFSharpConsole.exe is a command line tool to encode and decode CRF model. By default, the help information showed as follows:  
 Linear-chain CRF encoder & decoder by Zhongkai Fu (fuzhongkai@gmail.com)  
**CRFSharpConsole.exe** [parameter list...]  
**-encode** [parameter list...] - Encode CRF model from given training corpus
**-decode** [parameter list...] - Decode CRF model to label text
**-shrink** [parameter list...] - Shrink encoded CRF model size 

 As the above information shows, the tool provides two run modes. Encode mode is for training model, and decode mode is for testing model. The following paragraphs introduces how to use these two modes.

## Encode model
This mode is used to train CRF model from training corpus. Besides -encode parameter, the command line parameters as follows:  
**CRFSharpConsole.exe** -encode [parameters list]  
**-template** <filename>: template file name  
**-trainfile** <filename>: training corpus file name  
**-modelfile** <filename>: encoded model file name  
**-maxiter** <integer number>: maximum iteration, when encoding iteration reaches this value, the process will be ended. Default value is 1000  
**-minfeafreq** <integer number>: minimum feature frequency, if one feature's frequency is less than this value, the feature will be dropped. Default value is 2  
**-mindiff** <float-point number>: minimum diff value, when diff less than the value consecutive 3 times, the process will be ended. Default value is 0.0001  
**-thread** <integer number>: threads used to train model. Default value is 1  
**-slotrate** <float-point value>: the maximum slot usage rate threshold when building feature set. it is ranged in (0.0, 1.0). the higher value means longer time to build feature set, but smaller feature set size. Default value is 0.95  
**-hugelexmem** <integer>:  build lexical dictionary in huge mode and shrink starts when used memory reaches this value. This mode can build more lexical items, but slowly. Value ranges [1,100] and default is disabled.  
**-regtype** <type string>: regularization type. L1 and L2 regularization are supported. Default is L2  
**-retrainmodel** <string>: the existing model for re-training.  
**-debug**: encode model as debug mode  

Note: either -maxiter reaches setting value or -mindiff reaches setting value in consecutive three times, the training process will be finished and saved encoded model.

Note: -hugelexmem is only used for special task, and it is not recommended for common task, since it costs lots of time for memory shrink in order to load more lexical features into memory

A command line example as follows:
CRFSharpConsole.exe -encode -template template.1 -trainfile ner.train -modelfile ner.model -maxiter 100 -minfeafreq 1 -mindiff 0.0001 -thread 4 –debug

The entire encoding process contains four main steps as follows:  
1. Load train corpus from file, generate and select feature set according templates.  
2. Build selected feature set index data as double array trie-tree format, and save them into file.  
3. Run encoding process iteratively to tune feature values until reach end condition.  
4. Save encoded feature values into file.  
 In step 3, after run each iteration, some detailed encoding information will be show. For example:  
M_RANK_1 [FR=47658, TE=54.84%]  
M_RANK_2:27.07% M_RANK_0:26.65% E_RANK_0:0.31% B_RANK_0:0.21% E_RANK_1:0.19%  
iter=65 terr=0.320290 serr=0.717372 diff=0.0559666295793355 fsize=73762836(1.10% act) Time span: 00:31:56.4866295, Aver. time span per iter: 00:00:29  
The encoding information has two parts. The first part is information about each tag, the second part is information in overview.For each tag, it has two lines information. The first line shows the number of this tag in total (FR) and current token error rate (TE) about this tag. The second line shows this tag's token error distribution. In above example, in No.65 iteration, M_RANK_1 tag's token error rate is 54.84% in total. In these token error, 27.07% is M_RANK_2, 26.65% is M_RANK_0 and so on.For second part (information in overview), some global information is showed.   
**iter** : the number of iteration processed  
**terr** : tag's token error rate in all  
**serr** : record's error rate in all  
**diff** : different between current and previous iteration  
**fsize( x% act)** : the number of feature set in total, x% act means the number of non-zero value features. In L1 regularization, with the increasement of iter, x% is reduced. In L2 regularization, x% is always 100%.  
**Time span** : how long the encoding process has been taken  
**Aver. time span per iter** : the average time span for each iteration  

After encoding process is finished, the following files will be generated.  
 file1: **[model file name]**  
 This is model meta data file. It contains model's global parameters, feature templates, output tags and so on.    
 file2: **[model file name]**.feature  
 This is feature set lexical dictionary file. It contains all features's strings and corresponding ids. For high performance, it's built by double array tri-tree. In debug mode, [model file name].feature.raw_text which saves lexical dictionary in raw text will be generated.  
 file3: **[model file name]**.alpha  
 This is feature set weight score file. It contains all features' weight score.  

## Decode model
This mode is used to decode and test encoded model. Besides -decode parameter, there are some other required and optional parameters:  
CRFSharpConsole.exe -decode <options>  
**-modelfile** <string> : The model file used for decoding  
**-inputfile** <string> : The input file to predict its content tags  
**-outputfile** <string> : The output file to save predicted result  
**-nbest** <int> : Output n-best result, default value is 1  
**-prob** : output probability, default is not output  

Here is an example:  
 CRFSharpConsole.exe -decode -modelfile ner.model -inputfile ner_test.txt -outputfile ner_test_result.txt -nbest 5 -prob

## Shrink model
Encoded model with L1 regularization is usually a sparse model. Shrink parameter is used to reduce model file size. With -shrink parameter, the command line as follows:  
CRFSharpConsole.exe -shrink [Encoded CRF model file name] [Shrinked CRF model file name] [thread num]  
 An example as follows:  
CRFSharpConsole.exe -shrink ner.model ner_shrinked.model 16  
 This example is used to shrink ner.model files and the working thread is 16.   

## Incremental training
For some complex tasks, encoding model is timing-cost. With "-retrainmodel <previous encoded model file name>" option and updated training corpus (both old and new training corpus), CRFSharp supports to train model incrementally and compared with full training, incremental training is able to save lots of time.  There is an example:  
CRFSharpConsole.exe -encode -template template.1 -trainfile ner_new.train -modelfile ner_new.model -retrainmodel ner.model -maxiter 100 -minfeafreq 1 -mindiff 0.0001 -thread 4 –debug  

## Feature templates
CRFSharp template is totally compatible with CRF++ and used to generate feature set from training and testing corpus.

 In template file, each line describes one template which consists of prefix, id and rule-string. The prefix is used to indicate template type. There are two prefix, U for unigram template, and B for bigram template. Id is used to distinguish different templates. And rule-string is used to guide CRFSharp to generate features. 

 The rule-string has two types of form, one is constant string, and the other is macro. The simplest macro form is {“%x[row,col]”}. Row specifies the offset between current focusing token and generating feature token in row. Col specifies the absolute column position in corpus. Moreover, combined macro is also supported, for example: {“%x[row1, col1]/%x[row2, col2]”}. When generating feature set, macro will be replaced as specific string. A template file example as follows:

\# Unigram  
U01:%x[-1,0]  
U02:%x[0,0]  
U03:%x[1,0]  
U04:%x[-1,0]/%x[0,0]   
U05:%x[0,0]/%x[1,0]  
U06:%x[-1,0]/%x[1,0]  
U07:%x[-1,1]  
U08:%x[0,1]  
U09:%x[1,1]  
U10:%x[-1,1]/%x[0,1]  
U11:%x[0,1]/%x[1,1]  
U12:%x[-1,1]/%x[1,1]  
U13:C%x[-1,0]/%x[-1,1]   
U14:C%x[0,0]/%x[0,1]  
U15:C%x[1,0]/%x[1,1]  
\# Bigram  
B  

In this template file, it contains both unigram and bigram templates. Assuming current focusing token is “York NNP E_LOCATION” in the first record in training corpus above, the generated unigram feature set as follows:

U01:New  
U02:York  
U03:are  
U04:New/York  
U05:York/are  
U06:New/are  
U07:NNP  
U08:NNP  
U09:are  
U10:NNP/NNP  
U11:NNP/VBP  
U12:NNP/VBP  
U13:CNew/NNP  
U14:CYork/NNP  
U15:Care/VBP  

Although U07 and U08, U11 and U12’s rule-string are the same, we can still distinguish them by id string.

 In encoding process, according templates, encoder will generate feature set (like the example in above) from records in training corpus and save them into model file. 

 In decoding process, for each test record, decoder will also generate features by template, and check every feature whether it exists in model. If it is yes, feature’s alpha value will be applied while processing cost value.

 For each token, how many features will be generated from unigram templates? As the above said, if we have M unigram templates, each token will have M feature generated from the template set. Moreover, assuming each token has N different output classes, in order to indicate all possible statuses by binary function, we need to have {“M*N”} features for one token in total. For a record which contains L tokens, the feature size of this record is {“M*N*L”}.

For bigram template, CRFSharp will enumerate all possible combined output classes of two contiguous tokens, and generate features for each combined one. So, if each token has N different output classes, and the number of features generated by templates is M, the total bigram feature set size is {“N*N*M”}. For a record which contains L tokens, the feature size of this record is {“M*N*N*(L-1)”}.

## Run on Linux/Mac

With Mono-project which is the third party .NET framework on Linux/Mac, CRFSharp is able to run on some non-Windows platforms, such as Linux, Mac and others.

With NO_SUPPORT_PARALLEL_LIB flag, CRFSharp needn't to be re-compiled or modified to run on these operating systems. However, if you want to disable NO_SUPPORT_PARALLEL_LIB for the highest encoding performance, please modify existed code by replacing Parallel.For for "long" type with that for "int" type, since so far Mono-project hasn't implemented Parallel.For for "long" type yet.

## Use CRFSharp API in your project

Besides command line tool, CRFSharp provides APIs for developers to use it in their projects. In this section, we will show you how to use it. Basically, CRFSharp has two dll files: One is CRFSharp.dll which contains core algorithm and provides many APIs in low level. The other is CRFSharpWrapper.dll which wraps above low level interfaces and provides interfaces in high level.  

## Encode a CRF model in your project
1. Add CRFSharpWrapper.dll as reference  
2. Add following code snippet  
```c#
var encoder = new CRFSharpWrapper.Encoder();
var options = new EncoderArgs();
options.debugLevel = 1;
options.strTemplateFileName = "template.txt"; //template file name  
options.strTrainingCorpus = "train.txt"; //training corpus file name  
options.strEncodedModelFileName = "ner_model"; //encoded model file name  
options.max_iter = 1000;
options.min_feature_freq = 2;
options.min_diff = 0.0001;
options.threads_num = 4;
options.C = 1.0;
options.slot_usage_rate_threshold = 0.95;
bool bRet = encoder.Learn(options);
```
For detailed information, please visit source code: https://github.com/zhongkaifu/CRFSharp/blob/master/CRFSharpConsole/EncoderConsole.cs  

# Decode a CRFSharp model in your project
1. Add CRFSharpWrapper.dll as reference  
2. Add following code snippet  

```c#
//Create CRFSharp wrapper instance. It's a global instance
var crfWrapper = new CRFSharpWrapper.Decoder();

//Load encoded model from file
crfWrapper.LoadModel(options.strModelFileName);

//Create decoder tagger instance. If the running environment is multi-threads, each thread needs a separated instance
var tagger = crfWrapper.CreateTagger(options.nBest, options.maxword);
tagger.set_vlevel(options.probLevel);

//Initialize result
var crf_out = new crf_seg_out[options.nBest];
for (var i = 0; i < options.nBest; i++)
{
    crf_out[i] = new crf_seg_out(tagger.crf_max_word_num);
}

//Process  
List<List<string>> featureSet = BuildFeatureSet(strTestText); //Build feature set from given test text.
crfWrapper.Segment(crf_out, tagger, inbuf);

//An example for feature set builidng. Only use 1-dim character based feature  
privatestatic List<List<string>> BuildFeatureSet(string str)  
{  
    List<List<string>> sinbuf = new List<List<string>>();  
foreach (char ch in str)  
   {  
       sinbuf.Add(new List<string>());  
       sinbuf[sinbuf.Count - 1].Add(ch.ToString());  
   }  
return sinbuf;  
}  
```
The Decoder.Segment is a wrapped decoder interface. It's defined as follows:
```c#
 //Segment given text
 public int Segment(crf_out pout, //segment result
     SegDecoderTagger tagger, //Tagger per thread
     List<List<string>> inbuf, //feature set for segment
     )
```

#CRFSharp referenced by the following published papers  
1.     [Reconhecimento de entidades nomeadas em textos em português do Brasil no domınio do e-commerce](http://www.lbd.dcc.ufmg.br/colecoes/tilic/2015/010.pdf)  
2.     [Multimodal Wearable Sensing for Fine-Grained Activity Recognition in Healthcare](http://ieeexplore.ieee.org/xpls/abs_all.jsp?arnumber=7155432)  
3.     [A CRF-based Method for Automatic Construction of Chinese Symptom Lexicon](http://ieeexplore.ieee.org/xpls/abs_all.jsp?arnumber=7429085)  
4.     [Bileşik Cümlelerde Yan Cümleciklerin Otomatik Etiketlenmesi](http://ab.org.tr/ab16/bildiri/20.pdf)  
5.     [Entity Recognition in Bengali language](http://ieeexplore.ieee.org/xpls/abs_all.jsp?arnumber=7377333)  
6.     [A Hybrid Semi-supervised Learning Approach to Identifying Protected Health Information in Electronic Medical Records](http://dl.acm.org/citation.cfm?id=2857630)  
7.     [Global Journal on Technology](https://www.ce.yildiz.edu.tr/personal/mfatih/file/15131/x.pdf)  
8.     [Einf uhrung in Conditional Random Fields zum Taggen von sequentiellen Daten Tool: Wapiti](http://kitt.cl.uzh.ch/clab/crf/crf.pdf)  
9.     [Nghiên cứu phương pháp trích chọn thông tin thời tiết từ văn bản tiếng Việt](http://repository.vnu.edu.vn/bitstream/VNU_123/4980/1/00050005751.pdf)  
10.    [Unsupervised Word and Dependency Path Embeddings for Aspect Term Extraction](http://arxiv.org/abs/1605.07843)  
