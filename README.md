# AutoRDP

AutoRDP is a simple library/example how to deal with a very specific situation where you need to connect with windows RDP to another computer or virtual machine and it always prompts you for the credentials to login before connecting and for the computer's domain policies you cannot save the credential and always have to type manually the password and sometime both usernam and password.

## Usage

```python
Process.Start("cmd", "/c mstsc /v:<hostname> /w:1920 /h:1080");

var autoRDP = new AutoRDP.AutoRDP();
autoRDP.Login("username", "password", 10);
```

## Mentions

This code simply uses the already very useful library [InputSimulator](https://github.com/michaelnoonan/inputsimulator) to automatically insert the username and password in the prompt.

## License

[MIT](https://choosealicense.com/licenses/mit/)
