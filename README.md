# .NET-Ecommerce Web App

**Technologies:** ASP.NET Core 6, C#, SQL Server, ADO.NET, Vue.js, Bootstrap, Braintree

This application supports guests, registered customers, and admins, providing personalized experiences for returning users, feature-rich admin reporting, and product management tools. It integrates with Braintree for payment processing and employs layered security controls to protect application data.

## Features & Functionality

### User Experience
- Personalized homepage for returning customers based on purchase history.
- Shopping cart, product listings, and responsive design for mobile, tablet, and desktop devices.

### Admin Functionality
- Interactive reports on orders, products, and customers.
- Product management tools to add or edit products, with validation and secure database interactions.

### Security Measures
- HTTPS enforcement, secure cookie handling, and role-based access control.
- Input validation, cryptographically hashed passwords, and cross-site scripting prevention.
- Parameterized SQL queries to prevent injection attacks.

### Payment Integration
- Direct integration with Braintree for secure credit card and PayPal transactions.
- Tokens generated server-side for secure client-side payment collection.

### Full-Stack Development
- End-to-end development including database schema design, API integration, server-side and client-side logic, and testing.
- Server-side rendering (Razor Pages) when a page needs to be indexed by search engines (product listings page); otherwise client-side rendering (Vue.js).

### Architecture & Design
- Multi-tier architecture: client (browser), server (ASP.NET Core on IIS), DB (SQL Server).
- MVC pattern ensures clear separation of models, views, and controllers.
- Business logic encapsulated in backend objects for modularity and potential reusability across platforms.
- Session management handled securely using .NET Core session state to maintain shopping cart data and user sessions.

## Highlights & Impact
- Real-world experience with .NET, ADO.NET, and SQL Server.
- Implemented a recommendation algorithm to dynamically prioritize products for returning customers based on purchase similarity.
- Previously hosted publicly on SmarterASP.NET.