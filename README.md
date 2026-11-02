# Assignment: Completing the Blog Application

## Introduction

In the final phase of this project, our objective is to develop a comprehensive backend solution to complement the provided frontend. The backend must be fully functional, secure, and rigorously tested. It should incorporate middleware for handling authentication, authorization, and error management, along with unit tests for critical services and controllers.

The project includes placeholder controllers for student implementation, and the frontend components are already in place.

## Scenario

With the frontend views and empty controllers established, the next step is to implement the backend for the blog platform. The completed solution should support both anonymous readers and authenticated users, while offering enhanced capabilities for administrators and moderators.

## Frontend Requirements

### 1. MVC Setup and Layout

An MVC solution is provided featuring implemented views and empty controllers, but it does not include a data access layer.

For this task, ensure the following

#### Views Layout

- `_ViewStart.cshtml` and `_ViewImports.cshtml` are provided with appropriate common settings
- a responsive shared layout (`_Layout.cshtml`) is provided including:
   - A navigation bar that adapts to the user authentication state with Login/Register links for anonymous users and Logout/Change Password links for authenticated users.
   - Consistent styling using a CSS Framework (Boostrap, Tailwind, etc.) or custom CSS.
   - Mobile-friendly responsive design.
   - Footer with copyright information.

### 2. Authentication Views

User authentication interfaces are implemented with the following:

- Login page with email and password fields.
- User registration page for new readers.
- Logout confirmation page.
- All forms include appropriate validation.

### 3. Blog Features Views 

#### Home Page

A styled and responsive list displaying all blogs has been implemented, featuring the following fields:

- Blog title
- Subtitle
- Author's email
- The output of this page is cached for 30 minutes.

#### Individual Blog Details Page

This page must show the 5 most recent posts with:

- Post title.
- Publication date.
- A link to view the post details.
- A link to "View All Posts" that shows a complete list of blog posts.
- Clear navigation back to the home page.

#### Post Details Page

The page displays complete post information:

- Title
- Content
- Publication date
- Last modified date

The page also includes the comment section:

- existing comments are displayed chronologically (oldest to newest).
- a comment creation form is added only for authenticated users.
- unauthenticated users are prompted to log in or register to comment.
- show/hide buttons are included for comments (visible only to Admins and Moderators).

### 3. Author Features

Ensure a link to "My Blog" is added to the navigation bar for authors. This should lead to the blog details page.

#### Blog Details

The author must be allowed to edit their blog's title, subtitle, and description.

Include:

- A link to view all the posts is added.
- A link to create a new post is added.

#### Create Post

Ensure a form to create a new post is provided for the blog.

- Only an author can do this, and only for their blog.
- The form must validate all fields.
- The form must allow the post content to be HTML formatted, (https://summernote.org/ is added).

#### All Posts

This page will show all of the posts associated with the blog. Each post has an edit link.

#### Edit Post

This page allows editing of the title and content of the post. Again, only the author who owns the blog may do this.

## Backend Requirements

Implement the below controllers and their services:

### `CommunityController`
This controller should manage the blogs and posts with the following end points  
   - View Blog Details  
   - View Post Details  
   - Add Comments to Posts  
   - Toggle Comment Visibility  
   - View All Posts in a Blog

### `HomeController`   
This controller should fetch and cache the list of blogs.

### `PostController`  
This controller should manage posts.
   - List Posts for a User's Blog  
   - Create a New Post  
   - Edit an Existing Post

### `AccountController`  
This controller should manage all user actions:
   - Login a User  
   - Register a New User   
   - Logout a User

### `AuthAPIController`  
This controller should provide the following functionality:
   - Register an Admin User  
   - Register an Author User and Create an Associated Blog  
   - Register a Moderator User

### `BlogController`  
This controller should provide the following functionality:
   - View the Logged-in User's Blog  
   - Edit Blog Details


### Middleware Implementation

Implement middleware to handle:

- Authentication & Authorization
   - Ensure users are authenticated where required.  
   - Apply role-based access control (e.g., Moderators/Admins can toggle comment visibility).
- Global exception handling  
   - Capture unhandled exceptions and return structured error responses.
- Logging  
   - Log API requests, responses, and errors for debugging and auditing.


### Unit Testing

Write unit tests using XUnit and Moq for All Services And Controllers:

Test cases should cover:  

1. Valid and invalid inputs 
2. Authentication and authorization checks 
3. Expected exceptions and error handling

### Technical Requirements

1. Use ASP.NET Core MVC routing conventions.
2. Implement proper model validation.
3. Use appropriate authorization attributes for protected actions with claims.
4. Follow REST conventions for controller actions.
5. Implement proper error handling.
6. Use secure authentication practices.

## Testing Requirements

Verify the following scenarios:

1. Anonymous users can:
   - View all blogs and posts.
   - See visible comments.
   - Be prompted to log in when attempting to comment.

2. Authenticated users can:
   - View all content.
   - Add comments.
   - Manage their account.

3. Authors can:
   - View and edit blog details.
   - Create a new post.
   - Edit a post.

4. Moderators can:
   - Manage comment visibility.

## Starter Files

In this assignment, you will use the code provided to you in the `start/` directory.

## Submission Requirements

Submit a complete solution with all code files.

## Conclusion

This final phase combines all aspects of the blog platform, creating a cohesive and user-friendly experience while maintaining security. The completed project should demonstrate understanding of ASP.NET Core MVC, authentication/authorization.