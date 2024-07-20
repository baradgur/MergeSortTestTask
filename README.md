# MergeSortTestTask

## Disclaimers
1. The project is fully abandoned - tests on a real files became too time consuming (and I was afraid my SSD will suffer). I also have found a new job, so amount of free time decreased drastically. I am publishing this one in case I want to return to it someday - the challenge was a fun one
2. I know about the 'fast and easy' implementation. But based on a task description this solution would be not always a correct one, soe I was trying to optimize the preformance as a challenge


## Original task
The input is a large text file, where each line is a Number. String
For example:
415. Apple
30432. Something something something
1. Apple
32. Cherry is the best
2. Banana is yellow

Both parts can be repeated within the file. You need to get another file as output, where all
the lines are sorted. Sorting criteria: String part is compared first, if it matches then
Number.
Those in the example above, it should be:
1. Apple
415. Apple
2. Banana is yellow
32. Cherry is the best
30432. Something something something

You need to write two programs:
1. A utility for creating a test file of a given size. The result of the work should be a text file
of the type described above. There must be some number of lines with the same String
part.
2. The actual sorter. An important point, the file can be very large. The size of ~100Gb will
be used for testing.
When evaluating the completed task, we will first look at the result (correctness of
generation / sorting and running time), and secondly, at how the candidate writes the code.
Programming language: C#.
