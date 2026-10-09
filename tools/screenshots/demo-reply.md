## Fixing the login page

Here's what I changed, and why.

1. The form now remembers your email address between visits.
2. A clear message appears when the password is wrong, instead of the page just reloading.
3. The Sign in button is larger, so it's easier to hit on a phone.

```js
form.remember = true;
form.onError = showMessage;
```

Run the tests again and let me know if anything looks off. If the message wording isn't right, tell me what you'd like it to say.
