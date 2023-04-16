const PROXY_CONFIG = [
  {
    context: [
      "/purchases",
    ],
    target: "https://localhost:7110",
    secure: false
  }
]

module.exports = PROXY_CONFIG;
