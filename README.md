# End-to-end deduplication

This repository is dedicated to a set of complementary deduplication techniques that allow for ensuring end-to-end deduplication across not only component boundaries but also system boundaties.

## How it works?

The end-to-end deduplication techniques are based on two pillars: optimistic concurrency and one-time tokens.

### Optimistic concurrency

Optimistic concurrency control is a well-known technique for preventing lost writes when multiple users have access to the same set of data. The idea is very simple: you can modify the data only if it still is in the same state as when you read it. Most data strores support it, either by allowing to guard the update with check of the current state or by offering a version-like property that is returned on each read and can be provided to guard the write.

Optimistic concurrency technique greatly simpliefies the design of deduplication algorithms. A generic deduplication algorithm has the following flow:
 - Load the business state from the database
 - Check if message is not a duplicate
 - Update the business state and mark message as duplicate in the business state (optimistic concurrency write)
 - Mark message as processed in the deduplication state
 - Clear the message information from the business state

There is now clear distinction between ensuring exactly-once processing between attempts that do not overlap in time and ones that do overlap. If attempts do not overlap, the deduplication state (whatever it is and we will find out soon) is going to ensure no duplicate processing is allowed. If attempts do overlap in time that means that both attempts load the business state before one of them writes it. In that case the optimistic concurrency ensures only one attempt wins. The losing attempt is rolled back and, because message is not lost, another attempt is executed but this time it does not overlap.

### One-time tokens

Traditionally deduplication algorithms relied on information about messages that have already been processed. This usually meant that messages had to have unique IDs and these message IDs were stored in the deduplication state. The problem with that approach is that the deduplication store continuously grows in size. Theoretically no ID can ever be removed but in practices it is common to have cleanup procedures that remove entries older than a week or two. This is based on the assumption that duplicates that are so far apart in time are very unlikely. 

Apart from the fact that it does not give us 100% duplicate-free guarantee, the cleanup procedures are associated with other problems e.g. in certain data stores they are difficult to implement efficiently.

An alternative approach is to reverse the logic and use one-time processing tokens. As long as the token exists, the message can be processed. As part of marking the message as processed (see previous section), the token is removed so when a duplicate comes in, it is going to be ignored.


## Primary storage and side effects

All deduplication algorithms, regardless how they handle long-term memory of processed messages, need to initially mark a message as processed in the business data store as part of the actual business transaction. We call the data store that holds the business data and the short-term deduplication data **primary storage**. Note that some old algoritms use the primary storage also as long-term memory but this approach has some obvious downsides e.g. putting additional pressure on that resource.

In addition to updating the primary storage, a deduplication algorithm can have **side effects**. The simplest examples of a side effects include generated HTTP response when the algorithm is applied to HTTP endpoint or outgoing messages if the algorithm is applied to asynchronous message processing. It can also be a blob containing a PDF document that gets created during the transaction.

Side effects have two-stage lifecycle. First, they are generated during the business transaction. Before each side effects is *generated*, its ID is recorded in the deduplication state (stored in the primary storage). At this stage the side effect can't yet be visible to the outside world. If a given attempt is later rolled back, the generated side effect is deleted. Once the business transaction commits and the incoming signal is marked as processed, the side effects associated with a successful attempt are *published* and the ones associated with other attempts are deleted to avoid generating garbage in the database.

## Architecture

Architecture is about the big picture. This is about end-to-end deduplication so let's describe what the *end* means here. The environment consists of multiple software systems, each managed by an independent (or autonomous) organization. These systems share no resources. They can be deployed to different hosting environments and build on different platforms. Systems communicate via HTTP in two ways:
 - retrieving information stored in another system
 - triggering a state change in another system
 
### Services

Systems are broken down into services. Services form the middle level of the hieararchy. 


