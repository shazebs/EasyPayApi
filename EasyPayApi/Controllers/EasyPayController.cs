using EasyPayApi.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace EasyPayApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("default")]
    public class EasyPayController : ControllerBase
    {
        private readonly AzureBlobStorageService blobService;
        private readonly DatabaseService dbService;
        private readonly ILogger<EasyPayController> logger;

        public EasyPayController(ILogger<EasyPayController> _logger, DatabaseService _databaseService, AzureBlobStorageService _blobService)
        {
            logger = _logger;
            dbService = _databaseService;
            blobService = _blobService;
        }

        // ** API ENDPOINTS ** 

        /// <summary>
        /// Endpoint for handling a single checkout item without shipping details.
        /// </summary>
        /// <param name="salesOrderForm"></param>
        /// <returns></returns>
        [HttpPost("/checkout")]
        public IActionResult Checkout([FromBody] SalesOrderForm salesOrderForm)
        {
            try
            {
                // Get stripe key from the database associated with the username who listed the product.
                var stripe_key = dbService.GetStripeKey(salesOrderForm.username);

                using (var stripeService = new StripeService(stripe_key))
                {
                    // Generate a payment url checkout page using Stripe API.
                    var payment_url = stripeService.GeneratePaymentPortal_v2(salesOrderForm);

                    // Return best-case scenario for creating a Stripe payment checkout page.
                    return Ok(new
                    {
                        payment_url,
                        success = true
                    });
                }
            }
            // Handle any errors that may occur.
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    success = false
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="salesOrderForm"></param>
        /// <returns></returns>
        [HttpDelete("/catalog")]
        public IActionResult DeleteCatalogItem([FromBody] SalesOrderForm salesOrderForm)
        {
            try
            {
                List<SalesOrderForm> catalog;

                // Attempt to delete an item from catalog based on its ID.
                if (dbService.DeleteCatalogID(salesOrderForm, out catalog))
                {
                    return Ok(new
                    {
                        message = $"Catalog item '{salesOrderForm.name}' successfully deleted!",
                        success = true,
                        catalog
                    });
                }
                // A server error occurred while attempting to delete catalog item.
                else
                {
                    return BadRequest(new
                    {
                        message = $"Catalog item '{salesOrderForm.name}' failed to delete!",
                        success = false
                    });
                }
            }
            // Handle any errors that may occur.
            catch (Exception e)
            {
                return BadRequest(new
                {
                    message = e.Message,
                    success = false
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="photoUrl"></param>
        /// <returns></returns>
        [HttpDelete("/images")]
        public async Task<ActionResult> DeletePhoto([FromBody] string photoUrl)
        {
            try
            {
                // Attempt to delete photo from database.
                await blobService.DeletePhotoAsync(photoUrl);
                return NoContent(); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// (Deprecated) Original 'EasyPay' core functionality.
        /// </summary>
        /// <returns></returns>
        [HttpPost("/easypay")]
        public IActionResult EasyPay([FromBody] SalesOrder salesOrder)
        {
            try
            {
                // Generate a payment portal URL.
                string paymentportal = SalesOrderProcess(salesOrder);

                return Ok(new
                {
                    paymentportal,
                    success = true
                });
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    message = e.Message,
                    success = false,
                });
            }
        }

        /// <summary>
        /// Attempt to save a product to the database and return a list of catalog items.
        /// </summary>
        /// <param name="salesOrderForm"></param>
        /// <returns></returns>
        [HttpPost("/v2/easypay")]
        public IActionResult EasyPay_v2([FromBody] SalesOrderForm salesOrderForm)
        {
            try
            {
                // Try to save item and retrieve catalog.
                List<SalesOrderForm> catalog = dbService.AddToCatalog(salesOrderForm);
                return Ok(new
                {
                    catalog,
                    success = true
                });
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    message = e.Message,
                    success = false
                });
            }
        }

        /// <summary>
        /// For 'BlobPhotos.vue' list of photo urls.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/images")]
        public async Task<ActionResult<List<string>>> GetAllBlobPhotoUrls()
        {
            try
            {
                var photo_urls = await blobService.GetAllPhotoUrlsAsync();
                return Ok(new
                {
                    success = true,
                    photo_urls
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a list of catalog items associated with a requested username.
        /// </summary>
        /// <param name="getCatalog"></param>
        /// <returns></returns>
        [HttpPost("/catalog")]
        public IActionResult GetCatalog([FromBody] UserCatalogRequest userCatalogRequest)
        {
            try
            {
                List<SalesOrderForm> catalog = dbService.GetCatalogByUsername(userCatalogRequest.username);
                return Ok(new
                {
                    catalog,
                    success = true
                });
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    message = e.Message,
                    success = false
                });
            }
        }

        /// <summary>
        /// Return a registered user's 'email' and 'username' properties if login credentials are valid.
        /// </summary>
        /// <param name="loginForm"></param>
        /// <returns></returns>
        [HttpPost("/login")]
        public IActionResult Login([FromBody] LoginForm loginForm)
        {
            // Check database if login credentials are valid.
            Account account = dbService.LoginAccount(loginForm);

            // Login credentials are valid!
            if (account != null)
            {
                return Ok(new
                {
                    message = $"Account '{loginForm.username}' exists!",
                    success = true,
                    user = new
                    {
                        account.email,
                        account.username
                    }
                });
            }
            // Login credentials are not valid.
            else
            {
                return BadRequest(new
                {
                    message = "Account credentials are invalid!",
                    success = false,
                    user = account
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        [HttpPost("/register")]
        public IActionResult Register([FromBody] Account account)
        {
            try
            {
                // Check if username already exists before registering account.
                if (!dbService.CheckUsernameExists(account.username))
                {
                    // Username does not exists, attempt to register the account.
                    if (dbService.RegisterAccount(account))
                    {
                        // Return best-case scenario when registering account with EasyPay database.
                        return Ok(new
                        {
                            message = $"Account '{account.username}' is now registered with EasyPay!",
                            success = true
                        });
                    }
                    // A server error occurred with registering account!
                    else
                    {
                        return BadRequest(new
                        {
                            message = $"Account failed to register with EasyPay due to a server error!",
                            success = false
                        });
                    }
                }
                // Username already exists!
                else
                {
                    return Ok(new
                    {
                        message = $"Username '{account.username}' already exists!",
                        success = false
                    });
                }
            }
            // Handle any errors that may occur.
            catch (Exception e)
            {
                return BadRequest(new
                {
                    message = $"Account failed to register on EasyPay because: {e.Message}",
                    success = false
                });
            }
        }

        /// <summary>
        /// (Deprecated) Used with EasyPay endpoint.
        /// </summary>
        /// <param name="salesOrder"></param>
        /// <returns></returns>
        private string SalesOrderProcess(SalesOrder salesOrder)
        {
            using (var stripeService = new StripeService(salesOrder.StripeApiKey))
            {
                var paymentURL = stripeService.GeneratePaymentPortal(salesOrder);
                return paymentURL;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("/emails")]
        public IActionResult UpdateEmail([FromBody] UpdateEmailRequest request)
        {
            try
            {
                // Validate current password before attempting any new email updates.
                if (dbService.GetPasswordByUsername(request.username) != request.password)
                {
                    return Ok(new
                    {
                        message = $"Current Password is not correct!",
                        success = false
                    });
                }

                // Update email in the database.
                if (dbService.UpdateEmail(request))
                {
                    // Return best-case scenario for updating username.
                    Account? account = dbService.LoginAccount(new LoginForm
                    {
                        username = request.username,
                        password = request.password
                    });

                    // Return updated account.
                    if (account != null)
                    {
                        return Ok(new
                        {
                            success = true,
                            user = new
                            {
                                account.email,
                                account.username
                            }
                        });
                    }
                }
            }
            // Handle any exceptions thrown.
            catch (Exception e)
            {
                return BadRequest(new
                {
                    message = e.Message,
                    success = false
                });
            }

            // Handle any server error that may occur.
            return BadRequest(new
            {
                message = "A server error occurred!",
                success = false
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("/passwords")]
        public IActionResult UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            try
            {
                // Validate current password before attempting any new password updates.
                if (dbService.GetPasswordByUsername(request.username) != request.current_password)
                {
                    return Ok(new
                    {
                        message = $"Current Password is not correct!",
                        success = false
                    });
                }
                else
                {
                    // Attempt to update password in database.
                    if (dbService.UpdatePassword(request))
                    {
                        // Return best-case scenario for password update.
                        return Ok(new
                        {
                            message = "Password successfully changed!",
                            success = true
                        });
                    }
                }
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    message = e.Message,
                    success = false
                });
            }

            // Handle any server error that may occur.
            return BadRequest(new
            {
                message = "A server error occurred!",
                success = false
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("/stripekeys")]
        public IActionResult UpdateStripeKey([FromBody] UpdateStripeKeyRequest request)
        {
            try
            {
                // Validate current password before attempting any stripe key updates.
                if (dbService.GetPasswordByUsername(request.username) != request.password)
                {
                    return Ok(new
                    {
                        message = $"Current Password is not correct!",
                        success = false
                    });
                }

                // Attempt to update stripe key.
                if (dbService.UpdateStripeKey(request))
                {
                    // Return best-case scenario for updating stripe API key
                    return Ok(new
                    {
                        message = "Successfully updated Stripe API Key!",
                        success = true
                    });
                }
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    message = e.Message,
                    success = false
                });
            }

            // Handle any server error that may occur.
            return BadRequest(new
            {
                message = "A server error occurred!",
                success = false
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("/usernames")]
        public IActionResult UpdateUsername([FromBody] UpdateUsernameRequest request)
        {
            try
            {
                // Validate password before attempting any username updates.
                if (dbService.GetPasswordByUsername(request.current_username) != request.password)
                {
                    return Ok(new
                    {
                        message = $"Password is not valid!",
                        success = false
                    });
                }
                // Check if new username already exists. 
                if (dbService.CheckUsernameExists(request.new_username))
                {
                    return Ok(new
                    {
                        message = $"Username '{request.new_username}' already exists!",
                        success = false
                    });
                }
                else
                {
                    // Update username in accounts table.
                    if (dbService.UpdateUsername(request))
                    {
                        // Update username in catalog table.
                        if (dbService.UpdateUsernameInCatalog(request.current_username, request.new_username))
                        {
                            // Return best-case scenario for updating username.
                            Account? account = dbService.LoginAccount(new LoginForm
                            {
                                username = request.new_username,
                                password = request.password
                            });

                            // Return updated account.
                            if (account != null)
                            {
                                return Ok(new
                                {
                                    success = true,
                                    user = new
                                    {
                                        account.email,
                                        account.username
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    message = e.Message,
                    success = false
                });
            }

            return Ok(new
            {
                message = $"A server error occurred while updating username '{request.current_username}' to '{request.new_username}'",
                success = false
            });
        }

        /// <summary>
        /// Try to handle a request to upload an image to Azure Blob storage container account. 
        /// </summary>
        /// <returns></returns>
        [HttpPost("/images")]
        public async Task<IActionResult> UploadImage()
        {
            try
            {
                // Grab a reference to the image file from the request.
                var file = Request.Form.Files[0];

                using (var stream = file.OpenReadStream())
                {
                    // Create a unique filename for the requested image.
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                    // Make a call to blob service to upload the requested image.
                    var imageUrl = await blobService.UploadPhotoAsync(stream, fileName);

                    // Return a 200 status code with the newly uploaded image's url address.
                    return Ok(new
                    {
                        message = "File uploaded successfully",
                        success = true,
                        image_url = imageUrl
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Internal server error: {ex}",
                    success = false
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpPost("/webhook")]
        public async Task<ActionResult> WebhookHandler()
        {
            const string endpointSecret = "";
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            logger.LogWarning(json);
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], endpointSecret);

                // Handle the event
                if (stripeEvent.Type == Events.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;
                }
                // ... handle other event types
                else
                {
                    Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest();
            }
        }

    }
}