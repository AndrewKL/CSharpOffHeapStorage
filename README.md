# README #

This is a library to test to concept of off heap storage.  calling ToList() on large IEnumerable makes the garbage collector and the allocater unhappy.  This library pushes this data into an unmanaged memory stream. 

### How do I get set up? ###

*Download Repo
*Compile (packages should be included)