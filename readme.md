No need to specify any kind of identity as DefaultCredential() will do the job for you. 
Make sure to be connected to at least an account in the environment you're developing in : 
``` bash
az login 
```

2 Execution exist for the moment : 
- Audit : Will only log in the stdout what it would have done if set to Delete.
- Delete : Will actually delete the resource without further asking (to help automatize the task).

Coming Soon : 
- Notify, will let you send an email to ask for approval before executing deletion.