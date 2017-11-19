It will be a big plus to host trading system in service fabric stateful service and provide main operations in its interface


Trading Engine Test

------------------- Goal -------------------

Test candidate's problem solving skills and code style.

--------------- Prerequisite ---------------

Trading Engine is simplified version of FOREX trading system, it matches buy and sell orders placed by different counter parties. 

Every order has following properties:
Id - generate automatically
CurrencyPair - trading pair e.g. USD/EUR
Amount - the amount of order e.g. 100
Price - the price one of counter parties is willing to pay for given amount
Side - Buy or Sell

------------------- Task -------------------

Implement Trading Engine (TradingEngine.cs) and unit tests, so it satisfies following requirements:
- New orders should be matched with other orders, if possible. See order matching logic below
- Once order is placed, Trading Engine should raise OrderPlaced event
- Once order is fullfilled (fully matched with other orders), Trading Engine should raise OrderClosed event
- Trading Engine should be thread-safe and orders can be placed from different threads

-------------- Order matching --------------

Order can match ONLY:
- orders have the same CurrencyPair
- orders have opposite Side e.g. Buy order can match with Sell and vice versa 
- Buy order can be matched with Sell order, which have Price equal or lower than Buy order price
- Sell order can be matched with Buy order, which have Price equal or more than Sell order price

---------- Order Matching Example ----------

Imagine that current order books looks like below
+------+--------+--------+--------------+
| Side | Price  | Amount | CurrencyPair |
+------+--------+--------+--------------+
| BUY  | 560.50 |     50 | USD/EUR      |
| BUY  | 560.55 |     50 | USD/EUR      |
| BUY  | 560.60 |     40 | USD/EUR      |
| SELL | 580.60 |    100 | USD/EUR      |
+------+--------+--------+--------------+

now we place new order (Side=SELL, Price=560.53,Amount=80), acording to rules above this order can be fullfilled by BUY order with price => our order price,
so steps should be
Step1: match with (| BUY  | 560.55 |     50 | USD/EUR      |) for amount - 50, at this time BUY order is closed and we need to raise an event
Step2: match with (| BUY  | 560.60 |     40 | USD/EUR      |) for amount - 30, at this time SELL order is closed (because we fill 80 amount) and we need to raise an event 

------------------ Notes ------------------
