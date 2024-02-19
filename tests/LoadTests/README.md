# k6 Installation and Test Running Guide

## Installing k6

Follow these steps to install k6:

1. Download the k6 installer from the
[official k6 website](https://k6.io/docs/getting-started/installation).
2. Follow the installation instructions for your operating system.
3. Once installed, you can verify the installation by opening a terminal and
typing `k6 --version`. This should display the installed version of k6.

## Running Tests with k6

To run tests using a `test.js` file, follow these steps:

1. Ensure you have a JavaScript file named `test.js` with your k6 test script.
2. Open a terminal and navigate to the directory containing the `test.js` file.
3. Run the test by typing `k6 run test.js`.
4. k6 will execute the tests in the `test.js` file and display the results in
the terminal.
