using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Bowers.Models;
using Microsoft.AspNet.Identity;
using System.Web.Routing;
using System.Web.Helpers;
using Bowers.Areas.Participant.Models;
using Newtonsoft.Json.Linq;
using Stripe;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Bowers.Areas.Participant.Controllers
{
    [Authorize(Roles = "Participant")]
    public class CustomController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var userid = Guid.Parse(User.Identity.GetUserId());
            var participant = db.Participant.Where(x => x.Id == userid).FirstOrDefault();
            participant.LastLogin = DateTime.UtcNow.AddHours(-6);
            db.SaveChanges();

            if (participant.Status == "Verifying")
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Verifying",
                    action = "Index"
                }));
            }

            if (participant.Status == "Pending")
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Pending",
                    action = "Index"
                }));
            }

            if (participant.Status == "Denied")
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Denied",
                    action = "Index"
                }));
            }

        }
    }

    [Authorize(Roles = "Participant")]
    public class PageController : CustomController
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        decimal taxTotal = 0;

        public async Task APIPullAsync()
        {
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));

            Bowers.Models.Order order;
            order = db.Order.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");

            var pointtotal = participant.CalculatePoints(participant.Id);
            var ordertotal = order.CalculatePoints();
            var pointsneeded = ordertotal - pointtotal;
            var amount = pointsneeded * .025M;

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient httpClient = new HttpClient(handler);

            var url = "https://apiv2.octobat.com/tax_evidence_requests";

            var postInformation = new Dictionary<string, string>
                {
                    { "customer_billing_address_country", "US" },
                    { "customer_billing_address_state", participant.State },
                    { "customer_billing_address_zip", participant.Zip }
                };

            var byteArray = Encoding.ASCII.GetBytes("oc_test_skey_LJqACaBbCMmk53LokIHRVAtt");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            var content = JsonConvert.SerializeObject(postInformation);
            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
            HttpResponseMessage response = httpClient.PostAsync(url, stringContent).Result;

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<APIData>(responseContent);

                if (result.applied_rate != 0)
                {
                    taxTotal = amount * Convert.ToDecimal((result.applied_rate) / 100);
                    ViewBag.Tax = taxTotal;
                    ViewBag.Total = amount + taxTotal;
                }
                else
                {
                    ViewBag.Tax = 0;
                    ViewBag.Total = amount;
                }
            }
        }

        public async Task<decimal> APIPullTaxAsync()
        {
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));

            Bowers.Models.Order order;
            order = db.Order.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");

            var pointtotal = participant.CalculatePoints(participant.Id);
            var ordertotal = order.CalculatePoints();
            var pointsneeded = ordertotal - pointtotal;
            var amount = pointsneeded * .025M;

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient httpClient = new HttpClient(handler);

            var url = "https://apiv2.octobat.com/tax_evidence_requests";

            var postInformation = new Dictionary<string, string>
                {
                    { "customer_billing_address_country", "US" },
                    { "customer_billing_address_state", participant.State },
                    { "customer_billing_address_zip", participant.Zip }
                };

            var byteArray = Encoding.ASCII.GetBytes("oc_test_skey_LJqACaBbCMmk53LokIHRVAtt");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            var content = JsonConvert.SerializeObject(postInformation);
            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
            HttpResponseMessage response = httpClient.PostAsync(url, stringContent).Result;

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<APIData>(responseContent);

                if (result.applied_rate != 0)
                {
                    taxTotal = amount * Convert.ToDecimal((result.applied_rate) / 100);
                    ViewBag.Tax = taxTotal;
                    ViewBag.Total = amount + taxTotal;
                }
                else
                {
                    ViewBag.Tax = 0;
                    ViewBag.Total = amount;
                }
            }

            return taxTotal;
        }


        public async Task APIPullAsyncMDT(Guid Id)
        {
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));

            Bowers.Models.DTMOrder order;
            order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Id == Id);

            //var pointtotal = participant.CalculatePoints(participant.Id);
            //var ordertotal = order.CalculatePoints();
            // var pointsneeded = 

           
            var amount = order.CalculatePrice();

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient httpClient = new HttpClient(handler);

            var url = "https://apiv2.octobat.com/tax_evidence_requests";

            var postInformation = new Dictionary<string, string>
                {
                    { "customer_billing_address_country", "US" },
                    { "customer_billing_address_state", participant.State },
                    { "customer_billing_address_zip", participant.Zip }
                };

            var byteArray = Encoding.ASCII.GetBytes("oc_test_skey_LJqACaBbCMmk53LokIHRVAtt");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            var content = JsonConvert.SerializeObject(postInformation);
            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
            HttpResponseMessage response = httpClient.PostAsync(url, stringContent).Result;

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<APIData>(responseContent);

                if (result.applied_rate != 0)
                {
                    taxTotal = amount * Convert.ToDecimal((result.applied_rate) / 100);
                    ViewBag.Tax = taxTotal;
                    ViewBag.Total = amount + taxTotal;
                }
                else
                {
                    ViewBag.Tax = 0;
                    ViewBag.Total = amount;
                }
            }
        }

        public async Task<decimal> APIPullTaxAsyncMDT(Guid Id)
        {
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));

            Bowers.Models.DTMOrder order;
            order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Id == Id);

            //var pointtotal = participant.CalculatePoints(participant.Id);
            //var ordertotal = order.CalculatePoints();
            // var pointsneeded = 


            var amount = order.CalculatePrice();

            HttpClientHandler handler = new HttpClientHandler();
            HttpClient httpClient = new HttpClient(handler);

            var url = "https://apiv2.octobat.com/tax_evidence_requests";

            var postInformation = new Dictionary<string, string>
                {
                    { "customer_billing_address_country", "US" },
                    { "customer_billing_address_state", participant.State },
                    { "customer_billing_address_zip", participant.Zip }
                };

            var byteArray = Encoding.ASCII.GetBytes("oc_test_skey_LJqACaBbCMmk53LokIHRVAtt");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            var content = JsonConvert.SerializeObject(postInformation);
            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
            HttpResponseMessage response = httpClient.PostAsync(url, stringContent).Result;

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<APIData>(responseContent);

                if (result.applied_rate != 0)
                {
                    taxTotal = amount * Convert.ToDecimal((result.applied_rate) / 100);
                    ViewBag.Tax = taxTotal;
                    ViewBag.Total = amount + taxTotal;
                }
                else
                {
                    ViewBag.Tax = 0;
                    ViewBag.Total = amount;
                }
            }
            return taxTotal;
        }

        // GET: Participant/Page

        public string GetCatalogAccess()
        {
            var userid = Guid.Parse(User.Identity.GetUserId());
            var participant = db.Participant.Where(x => x.Id == userid).FirstOrDefault();

            return participant.CatalogAccess;

        }
        public ActionResult Index()
        {
            var userid = Guid.Parse(User.Identity.GetUserId());
            var participant = db.Participant.Where(x => x.Id == userid).FirstOrDefault();
            ViewBag.Participant = participant;
            ViewBag.FirstName = participant.FirstName;
            return View();
        }

        public ActionResult DTMIndex()
        {
            var userid = Guid.Parse(User.Identity.GetUserId());
            var participant = db.Participant.Where(x => x.Id == userid).FirstOrDefault();
            ViewBag.Participant = participant;
            ViewBag.FirstName = participant.FirstName;
            return View();
        }

        public ActionResult ChooseCatalog()
        {
            var userid = Guid.Parse(User.Identity.GetUserId());
            var participant = db.Participant.Where(x => x.Id == userid).FirstOrDefault();

            ViewBag.FirstName = participant.FirstName;
            ViewBag.UserId = userid;
            return View();
        }

        [HttpPost]
        public JsonResult GetUserIDUpdateSSN(string id, int ssn)
        {
            Bowers.Models.Participant participant = new Bowers.Models.Participant();
            participant = db.Participant.Where(x => x.Id.ToString().ToLower() == id.ToLower()).FirstOrDefault();
            var formattedSSN = String.Format("{0:000-00-0000}", ssn);
            participant.SetSocialSecurityNumber(formattedSSN);

            //if (String.IsNullOrEmpty(ssn.ToString()))
            //{
            //    ModelState.AddModelError("", "SSN Cannot be blank!");
            //}

            db.SaveChanges();

            return Json(participant);
        }

        public string GetFirstName()
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            return participant.FirstName;
        }

        public decimal GetPoints()
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            return participant.CalculatePoints(participant.Id);
        }

        public decimal GetPointsDTM()
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            return participant.CalculatePointsDTM(participant.Id);
        }

        public int DisplayCart()
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var cart = db.Order.Where(x => x.Status == "Unsubmitted" && x.Participant.Id == participant.Id);
            var count = cart.Count();
            return cart.Count();
        }
        
        public int DisplayMDTCart()
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var cart = db.DTMOrder.Where(x => x.Status == "Unsubmitted" && x.Participant.Id == participant.Id);
            var count = cart.Count();
            return cart.Count();
        }

        public ActionResult Claim(string filtertype, string filterstyle)
        {
            ClaimHeaderModel model = new ClaimHeaderModel();
            var today = DateTime.Parse(DateTime.UtcNow.AddHours(-6).ToShortDateString());
            var promotions = db.Promotion.Where(x => x.DateFrom <= today && x.DateTo >= today && x.Inactive == false).OrderBy(x => x.Style).AsEnumerable();
            var types = db.Promotion.Where(x => x.DateFrom <= today && x.DateTo >= today && x.Inactive == false).OrderBy(x => x.Type).Select(x => x.Type).Distinct();
            ViewBag.Types = types;
            if (!String.IsNullOrEmpty(filtertype))
            {
                promotions = promotions.Where(x => x.Type == filtertype);
            }

            if (!String.IsNullOrEmpty(filterstyle))
            {
                promotions = promotions.Where(x => x.Style == filterstyle);
            }

            ViewBag.ProductList = new WebGrid(promotions, canPage: true, canSort: true);

            return View(model);
        }

        [HttpPost]
        public JsonResult CheckTerms()
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var result = participant.NewTC;           


            return Json(result);
        }

        public JsonResult UpdateTerms() 
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            participant.NewTC = true;
            db.SaveChanges();


            return Json(true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Claim(ClaimHeaderModel model, string type, string filterstyle)
        {
            var today = DateTime.Parse(DateTime.UtcNow.AddHours(-6).ToShortDateString());
            var promotions = db.Promotion.Where(x => x.DateFrom <= today && x.DateTo >= today && x.Inactive == false).OrderBy(x => x.Style).AsEnumerable();
            var types = db.Promotion.Where(x => x.DateFrom <= today && x.DateTo >= today && x.Inactive == false).OrderBy(x => x.Type).Select(x => x.Type).Distinct();
            ViewBag.Types = types;
            if (!String.IsNullOrEmpty(type))
            {
                promotions = promotions.Where(x => x.Type == type);
            }

            if (!String.IsNullOrEmpty(filterstyle))
            {
                promotions = promotions.Where(x => x.Style == filterstyle);
            }

            ViewBag.ProductList = new WebGrid(promotions, canPage: true, canSort: true);

            if (ModelState.IsValid)
            {
                if (db.Promotion.Any(x => x.DateFrom <= today && x.DateTo >= today && x.Inactive == false && x.Style == model.Style && x.Type == model.Type))
                {
                    if (model.DateSold > today)
                    {
                        ModelState.AddModelError("", "Invalid Date.  Cannot be greater than today.");
                    }
                    else
                    {
                        var maxid = 0;
                        if (db.Claim.Any())
                        {
                            maxid = db.Claim.Max(x => x.ClaimID);
                        }
                        var userid = User.Identity.GetUserId();
                        var participant = db.Participant.Find(new Guid(userid));
                        var promotion = db.Promotion.Where(x => x.DateFrom <= today && x.DateTo >= today && x.Inactive == false && x.Style == model.Style && x.Type == model.Type).FirstOrDefault();
                        var newclaim = new Claim();
                        newclaim.Id = Guid.NewGuid();
                        newclaim.Participant = participant;
                        newclaim.Promotion = promotion;
                        newclaim.Quantity = model.Quantity;
                        newclaim.RewardAmount = promotion.RewardAmount * model.Quantity;
                        newclaim.StatusDate = today;
                        newclaim.Style = model.Style;
                        newclaim.Type = model.Type;
                        newclaim.DateCreated = today;
                        newclaim.DateSold = model.DateSold ?? today;
                        newclaim.DateSubmitted = today;
                        newclaim.ClaimID = maxid + 1;
                        newclaim.Status = "Approved";
                        db.Claim.Add(newclaim);
                        db.SaveChanges();
                        ViewBag.Completed = 1;
                        ViewBag.Claim = newclaim.ClaimID;
                        ViewBag.Reward = newclaim.RewardAmount;
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid Type and/or Style # Entered");
                }

            }

            return View(model);
        }

        public ActionResult CatalogCategory()
        {
            return View();
        }

        public ActionResult Catalog(string gender, string type, string filterstyle)
        {
            var catalog = db.Catalog.Where(x => x.Status == "Available");

            if (!String.IsNullOrEmpty(filterstyle))
            {
                catalog = catalog.Where(x => x.Model.Contains(filterstyle));
            }

            ViewBag.Catalog = catalog.ToList().OrderBy(x => x.SortOrder).Select(x => new CatalogModel { Style = x.Model, Id = x.Id, RewardAmount = x.RewardAmount, ImageLocation = x.ImageLocation, UofM = x.UofM }); ;
            return View();
        }

        public ActionResult DTMCatalog(string search, string available)
        {
            var catalog = GetCatalog();
            ViewBag.Search = "";

            if (!String.IsNullOrEmpty(search))
            {
                catalog = catalog.Where(x => x.Brand.ToLower().Contains(search.ToLower()) || x.Model.ToLower().Contains(search.ToLower()) || x.Category.ToLower().Contains(search.ToLower())).ToList();
                ViewBag.Search = search;
            }
            if (!String.IsNullOrEmpty(available)) 
            {
                var value = Boolean.Parse(available);
                catalog = catalog.Where(x => x.Available == value).ToList();
                ViewBag.Availability = value.ToString();
            }

            var cat = catalog.OrderBy(x => x.Brand).ThenBy(e => e.Category).ThenBy(j => j.Model).Select(y => new DTMCatalogModel { Brand = y.Brand, Category = y.Category, Model = y.Model, Id = y.Id, ModelVariation = y.ModelVariation, MRSP = y.MRSP, BestPrice = y.BestPrice, Points = y.Points });
            ViewBag.WebGrid = new WebGrid(cat, rowsPerPage: 25);
            return View();
        }

        public List<DTMCatalogModel> GetCatalog()
        {
            var points = GetPointsDTM();
            var catalog = db.DTMCatalog.Where(x => x.Status == true).ToList();

            List<DTMCatalogModel> catalogList = new List<DTMCatalogModel>();

            foreach (var item in catalog)
            {
                var catalogitem = new DTMCatalogModel();

                catalogitem.Id = item.Id;
                catalogitem.Model = item.Model;
                catalogitem.ModelVariation = item.ModelVariant;
                catalogitem.MRSP = item.MSRP;
                catalogitem.Brand = item.Brand;
                catalogitem.Category = item.Category;
                catalogitem.Available = item.Availability;



                if (points >= item.Points100)
                {
                    catalogitem.BestPrice = item.Price100;
                    catalogitem.Points = item.Points100;

                    catalogList.Add(catalogitem);
                    continue;
                }
                else if (points >= item.Points95)
                {
                    catalogitem.BestPrice = item.Price95;
                    catalogitem.Points = item.Points95;

                    catalogList.Add(catalogitem);
                    continue;
                }
                else if (points >= item.Points90)
                {
                    catalogitem.BestPrice = item.Price90;
                    catalogitem.Points = item.Points90;

                    catalogList.Add(catalogitem);
                    continue;
                }
                else if (points >= item.Points85)
                {
                    catalogitem.BestPrice = item.Price85;
                    catalogitem.Points = item.Points85;

                    catalogList.Add(catalogitem);
                    continue;
                }
                else if (points >= item.Points80)
                {
                    catalogitem.BestPrice = item.Price80;
                    catalogitem.Points = item.Points80;

                    catalogList.Add(catalogitem);
                    continue;
                }
                else if (points >= item.Points75)
                {
                    catalogitem.BestPrice = item.Price75;
                    catalogitem.Points = item.Points75;

                    catalogList.Add(catalogitem);
                    continue;
                }
                else
                {
                    catalogitem.BestPrice = item.MSRP;
                    catalogitem.Points = 0.0M;

                    catalogList.Add(catalogitem);
                    continue;
                }

            }

            return catalogList;

        }

        public ActionResult SelectAccount()
        {
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));

            if (participant.CatalogAccess == "B&W")
            {
                return RedirectToAction("Account", "Page");
            }
            else if (participant.CatalogAccess == "M&DT")
            {
                return RedirectToAction("DTMAccount", "Page");
            }

            return View();
        }

        public ActionResult Account()
        {

            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var earn = db.Claim.Where(x => x.Participant.Id == participant.Id).OrderByDescending(x => x.ClaimID);
            ViewBag.Earn = new WebGrid(earn, canPage: true, canSort: true, rowsPerPage: 20);
            var redeem = db.ProductinOrder.Where(x => x.Order.Participant.Id == participant.Id && x.Order.Status != "Unsubmitted").OrderByDescending(x => x.Order.SequentialId).ThenBy(x => x.Id).ToList();
            ViewBag.Redeem = new WebGrid(redeem, canPage: true, canSort: true, rowsPerPage: 20);
            var purchase = db.PointTransaction.Where(x => x.Participant.Id == participant.Id && x.Status == "succeeded").OrderByDescending(x => x.DateCreated).ToList();
            ViewBag.PurchaseCount = purchase.Count();
            ViewBag.Purchase = new WebGrid(purchase, canPage: true, canSort: true);
            return View();
        }


        public ActionResult DTMAccount()
        {

            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var earn = db.Claim.Where(x => x.Participant.Id == participant.Id && x.Catalog == "DT&M").OrderByDescending(x => x.ClaimID);
            ViewBag.Earn = new WebGrid(earn, canPage: true, canSort: true, rowsPerPage: 20);
            var redeem = db.DTMProductinOrder.Where(x => x.DTMOrder.Participant.Id == participant.Id && x.DTMOrder.Status != "Unsubmitted").OrderByDescending(x => x.DTMOrder.SequentialId).ThenBy(x => x.Id).ToList();
            ViewBag.Redeem = new WebGrid(redeem, canPage: true, canSort: true, rowsPerPage: 20);

            return View();
        }

        [HttpGet]
        public ActionResult DTMProduct(Guid id)
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));

            var products = GetProductBestPrice(id);

            ViewBag.Products = products;
            ViewBag.Participant = participant;
            return View();
        }

        [HttpPost]
        public ActionResult DTMProduct(Guid id, string msrp)
        {

            int quantity = 1;

            var db = new ApplicationDbContext();
            var products = db.DTMCatalog.Find(id);
            // var productsizes = db.CatalogSize.Where(x => x.Catalog.Id == products.Id && x.Status == "Available").OrderBy(x => x.SortOrder).ThenBy(x => x.Finish);
            ViewBag.Products = products;
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            ViewBag.Participant = participant;
            // ViewBag.Sizes = productsizes;
            if (products != null)
            {
                // var product = db.CatalogSize.Find(new Guid(SizeId));
                Bowers.Models.DTMOrder order;
                try
                {
                    order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");

                    //var pointtotal = participant.CalculatePoints(participant.Id);
                    //var ordertotal = order.CalculatePoints() + products.;
                    //if (pointtotal < ordertotal)
                    //{
                    //    ModelState.AddModelError("", "You don’t have enough points to complete your transaction! You must remove items from your cart to complete your order.");

                    //    var p = GetProductBestPrice(id);
                    //    ViewBag.Products = p;
                    //    ViewBag.Participant = participant;
                    //    return View();
                    //}

                    var productsinorder = db.DTMProductinOrder.Where(x => x.DTMOrder.Id == order.Id).ToList();

                    foreach (var item in productsinorder)
                    {
                        if (item.Product.Availability != products.Availability)
                        {                         

                            ModelState.AddModelError("", "You must submit an order for the items with the same Availability first.");

                            var p = GetProductBestPrice(id);
                            ViewBag.Products = p;
                            ViewBag.Participant = participant;
                            return View();
                        }

                        if (productsinorder.Count >= 1 && products.Availability == false)
                        {
                            ModelState.AddModelError("", "You can only have 1 Unavailable item in your cart at a time!");

                            var p = GetProductBestPrice(id);
                            ViewBag.Products = p;
                            ViewBag.Participant = participant;
                            return View();
                        }
                    }

                }
                catch
                {
                    order = new Bowers.Models.DTMOrder(participant);
                    db.DTMOrder.Add(order);
                }


                DTMProductinOrder dtmproductInOrder;
                try
                {
                    dtmproductInOrder = order.DTMProductsInOrder.First(e => e.Product.Id == products.Id);
                    dtmproductInOrder.Quantity += quantity;

                    if (Decimal.Parse(msrp) == products.Points75)
                    {
                        dtmproductInOrder.PurchaseOption = "75%";
                    }
                    else if (Decimal.Parse(msrp) == products.Points80)
                    {
                        dtmproductInOrder.PurchaseOption = "80%";
                    }
                    else if (Decimal.Parse(msrp) == products.Points85)
                    {
                        dtmproductInOrder.PurchaseOption = "85%";
                    }
                    else if (Decimal.Parse(msrp) == products.Points90)
                    {
                        dtmproductInOrder.PurchaseOption = "90%";
                    }
                    else if (Decimal.Parse(msrp) == products.Points95)
                    {
                        dtmproductInOrder.PurchaseOption = "95%";
                    }
                    else if (Decimal.Parse(msrp) == products.Points100)
                    {
                        dtmproductInOrder.PurchaseOption = "100%";
                    }
                    else
                    {
                        dtmproductInOrder.PurchaseOption = "0%";
                    }
                }
                catch
                {
                    dtmproductInOrder = new DTMProductinOrder(products, order);
                    db.DTMProductinOrder.Add(dtmproductInOrder);
                    dtmproductInOrder.Quantity = quantity;
                    if (Decimal.Parse(msrp) == products.Points75)
                    {
                        dtmproductInOrder.PurchaseOption = "75%";
                    }
                    else if (Decimal.Parse(msrp) == products.Points80)
                    {
                        dtmproductInOrder.PurchaseOption = "80%";
                    }
                    else if (Decimal.Parse(msrp) == products.Points85)
                    {
                        dtmproductInOrder.PurchaseOption = "85%";
                    }
                    else if (Decimal.Parse(msrp) == products.Points90)
                    {
                        dtmproductInOrder.PurchaseOption = "90%";
                    }
                    else if (Decimal.Parse(msrp) == products.Points95)
                    {
                        dtmproductInOrder.PurchaseOption = "95%";
                    }
                    else if (Decimal.Parse(msrp) == products.Points100)
                    {
                        dtmproductInOrder.PurchaseOption = "100%";
                    }
                    else
                    {
                        dtmproductInOrder.PurchaseOption = "0%";
                    }
                }

                db.SaveChanges();
            }
            else
            {
                throw new HttpException(404, "Product not found");
            }

            return RedirectToAction("MDTCart", "Page");
        }

        public ActionResult MDTCart(string over)
        {


            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            Bowers.Models.DTMOrder order;

            try
            {
                order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");
            }
            catch
            {
                order = new Bowers.Models.DTMOrder(participant);
                db.DTMOrder.Add(order);
                db.SaveChanges();
            }

            //try
            //{
            //    foreach (var productInOrder in order.DTMProductsInOrder)
            //    {
            //        if (productInOrder.Product.Status != true)
            //        {
            //            order.DTMProductsInOrder.Remove(productInOrder);
            //        }

            //    }
            //}
            //catch
            //{
            //}

            db.SaveChanges();

            ViewBag.Order = order;
            ViewBag.Participant = participant;

            var pid = new Guid(User.Identity.GetUserId());

            ViewBag.PointTotal = participant.CalculatePointsDTM(participant.Id);

            var pointtotal = participant.CalculatePointsDTM(participant.Id);
            var ordertotal = order.CalculatePoints();
            if (pointtotal < ordertotal)
            {
                ViewBag.Over = "You do not have enough points to complete your order.";
            }

            if (over == "True")
            {
                ViewBag.Over = "You do not have enough points to complete your order.";
            }

            return View();
        }




        public DTMProductModel GetProductBestPrice(Guid id)
        {
            var points = GetPointsDTM();
            var catalog = db.DTMCatalog.Find(id);

            DTMProductModel catalogItem = new DTMProductModel();

            catalogItem.Id = catalog.Id;
            catalogItem.Model = catalog.Model;
            catalogItem.ModelVariation = catalog.ModelVariant;
            catalogItem.MRSP = catalog.MSRP;
            catalogItem.Available = catalog.Availability;
            catalogItem.Brand = catalog.Brand;
            catalogItem.Category = catalog.Category;
            catalogItem.UoM = catalog.UoM;

            if (points >= catalog.Points100)
            {
                catalogItem.Price100 = catalog.Price100;
                catalogItem.Points100 = catalog.Points100;

            }
            if (points >= catalog.Points95)
            {
                catalogItem.Price95 = catalog.Price95;
                catalogItem.Points95 = catalog.Points95;

            }
            if (points >= catalog.Points90)
            {
                catalogItem.Price90 = catalog.Price90;
                catalogItem.Points90 = catalog.Points90;

            }
            if (points >= catalog.Points85)
            {
                catalogItem.Price85 = catalog.Price85;
                catalogItem.Points85 = catalog.Points85;

            }
            if (points >= catalog.Points80)
            {
                catalogItem.Price80 = catalog.Price80;
                catalogItem.Points80 = catalog.Points80;

            }
            if (points >= catalog.Points75)
            {
                catalogItem.Price75 = catalog.Price75;
                catalogItem.Points75 = catalog.Points75;

            }
            if (points < catalog.Points75)
            {
                catalogItem.BestPrice = 0.1M;
                catalogItem.Points = 0.1M;

            }


            return catalogItem;

        }

        public ActionResult Product(Guid id)
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));

            var products = db.Catalog.Find(id);

            if (products.Status != "Available")
            {
                return View("Index");
            }

            var productsizes = db.CatalogSize.Where(x => x.Catalog.Id == products.Id && x.Status == "Available").OrderBy(x => x.SortOrder).ThenBy(x => x.Finish);

            ViewBag.Sizes = productsizes;
            ViewBag.Products = products;
            ViewBag.Participant = participant;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Product(Guid Id, string Button, string SizeId)
        {

            int quantity = 1;

            var db = new ApplicationDbContext();
            var products = db.Catalog.Find(Id);
            var productsizes = db.CatalogSize.Where(x => x.Catalog.Id == products.Id && x.Status == "Available").OrderBy(x => x.SortOrder).ThenBy(x => x.Finish);
            ViewBag.Products = products;
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            ViewBag.Participant = participant;
            ViewBag.Sizes = productsizes;
            if (products != null)
            {
                if (String.IsNullOrEmpty(SizeId))
                {
                    ModelState.AddModelError("", "Please Select Finish");
                    return View();
                }
                else
                {

                    var product = db.CatalogSize.Find(new Guid(SizeId));
                    Bowers.Models.Order order;
                    try
                    {
                        order = db.Order.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");
                    }
                    catch
                    {
                        order = new Bowers.Models.Order(participant);
                        db.Order.Add(order);
                    }


                    ProductinOrder productInOrder;
                    try
                    {
                        productInOrder = order.ProductsInOrder.First(e => e.Product.ItemId == product.ItemId);
                        productInOrder.Quantity += quantity;
                    }
                    catch
                    {
                        productInOrder = new ProductinOrder(product, order);
                        db.ProductinOrder.Add(productInOrder);
                        productInOrder.Quantity = quantity;
                    }

                    db.SaveChanges();
                }
            }
            else
            {
                throw new HttpException(404, "Product not found");
            }

            return RedirectToAction("Cart", "Page");
        }

        public ActionResult Cart(string over)
        {

            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            Bowers.Models.Order order;

            try
            {
                order = db.Order.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");
            }
            catch
            {
                order = new Bowers.Models.Order(participant);
                db.Order.Add(order);
                db.SaveChanges();
            }

            try
            {
                if (order.ProductsInOrder != null) 
                {

                    foreach (var productInOrder in order.ProductsInOrder)
                    {
                        if (productInOrder.Product.Status != "Available")
                        {
                            order.ProductsInOrder.Remove(productInOrder);
                        }

                    }
                }
                
            }
            catch
            {
            }

            db.SaveChanges();

            ViewBag.Order = order;
            ViewBag.Participant = participant;

            var pid = new Guid(User.Identity.GetUserId());

            ViewBag.PointTotal = participant.CalculatePoints(participant.Id);

            var pointtotal = participant.CalculatePoints(participant.Id);
            var ordertotal = order.CalculatePoints();
            if (pointtotal < ordertotal || (pointtotal - ordertotal) < 0 || pointtotal < 0)
            {
                ViewBag.Over = "You do not have enough points to complete your order.";
            }

            if (over == "True")
            {
                ViewBag.Over = "You do not have enough points to complete your order.";
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(Guid id, decimal quantity, string note)
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var productInOrder = db.ProductinOrder.Find(id);

            if (productInOrder.IsUpdateAllowed(participant))
            {
                if (quantity > 0)
                {
                    productInOrder.Note = note;
                    productInOrder.Quantity = quantity;
                }
                else
                {
                    db.ProductinOrder.Remove(productInOrder);
                }
                db.SaveChanges();
            }

            return RedirectToAction("Cart", "Page");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MDTUpdateQuantity(Guid id, decimal quantity, string note)
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var productInOrder = db.DTMProductinOrder.Find(id);

            //if (productInOrder.Product.Availability == false)
            //{
            //    ModelState.AddModelError("", "You can only have 1 Unavailable item in your cart at a time!");
            //    return RedirectToAction("MDTCart", "Page");
            //}

            if (productInOrder.IsUpdateAllowed(participant))
            {
                if (quantity > 0)
                {
                    productInOrder.Note = note;
                    productInOrder.Quantity = quantity;
                }
                else
                {
                    db.DTMProductinOrder.Remove(productInOrder);
                }
                db.SaveChanges();
            }

            return RedirectToAction("MDTCart", "Page");
        }

        public ActionResult MDTCheckout()
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            try
            {
                Bowers.Models.DTMOrder order;
                order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");

                //try
                //{
                //    foreach (var productInOrder in order.DTMProductsInOrder)
                //    {
                //        if (productInOrder.Product.Status != true)
                //        {
                //            order.DTMProductsInOrder.Remove(productInOrder);
                //            var fredemption = db.DTMProductinOrder.Where(x => x.Status != "Cancelled");

                //        }

                //    }
                //}
                //catch
                //{
                //}

                //db.SaveChanges();

                ViewBag.Order = order;

                var pid = new Guid(User.Identity.GetUserId());

                var pointtotal = participant.CalculatePointsDTM(participant.Id);

                ViewBag.PointTotal = pointtotal;

                var ordertotal = order.CalculatePoints();
                var orderpricetotal = order.CalculatePrice();


                if (pointtotal < ordertotal)
                {
                    return RedirectToAction("MDTCart", new { over = true });
                }

                var products = from p in db.DTMProductinOrder.Where(x => x.DTMOrder.Id == order.Id)
                               select new
                               {
                                   Id = p.Product.Id,
                                   ProductName = p.Product.Model,
                                   Quantity = p.Quantity,
                                   MSRP = p.PurchaseOption,
                                   Available = p.Product.Availability,
                                   Brand = p.Product.Brand,
                                   Category = p.Product.Category,


                               };
                List<DTMCheckout> productlist = new List<DTMCheckout>();

                foreach (var item in products)
                {
                    var finalprice = GetFinalPrice(item.Id, item.MSRP);
                    var finalpoints = GetFinalPoints(item.Id, item.MSRP);

                    var list = new DTMCheckout()
                    {
                        Id = item.Id,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        MSRP = item.MSRP,
                        Available = item.Available == true ? "Available" : "Unavailable",
                        Price = finalprice * item.Quantity,
                        Points = finalpoints * item.Quantity,
                        Brand = item.Brand,
                        Category = item.Category
                    };

                    productlist.Add(list);
                }

                ViewBag.WebGrid = new WebGrid(productlist, rowsPerPage: 10);
                ViewBag.OrderTotal = ordertotal;
                ViewBag.OrderTotalPrice = orderpricetotal;

                if (products.Count() == 0)
                {
                    return Redirect("Index");
                }

            }
            catch (Exception e)
            {
                return Redirect("Index");
            }

            CheckoutModel model = new CheckoutModel();
            model.Country = "us";
            return View(model);

        }

        public decimal GetFinalPrice(Guid id, string percentage)
        {
            var price = 0M;
            var db = new ApplicationDbContext();
            var item = db.DTMCatalog.Find(id);


            if (percentage == "75%")
            {
                price = item.Price75;
            }
            else if (percentage == "80%")
            {
                price = item.Price80;
            }
            else if (percentage == "85%")
            {
                price = item.Price85;
            }
            else if (percentage == "90%")
            {
                price = item.Price90;
            }
            else if (percentage == "95%")
            {
                price = item.Price95;
            }
            else if (percentage == "100%")
            {
                price = item.Price100;
            }

            return price;

        }

        public decimal GetFinalPoints(Guid id, string percentage)
        {
            var price = 0M;
            var db = new ApplicationDbContext();
            var item = db.DTMCatalog.Find(id);


            if (percentage == "75%")
            {
                price = item.Points75;
            }
            else if (percentage == "80%")
            {
                price = item.Points80;
            }
            else if (percentage == "85%")
            {
                price = item.Points85;
            }
            else if (percentage == "90%")
            {
                price = item.Points90;
            }
            else if (percentage == "95%")
            {
                price = item.Points95;
            }
            else if (percentage == "100%")
            {
                price = item.Points100;
            }

            return price;

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MDTCheckout(CheckoutModel model)
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));

            var order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");        
          
            ViewBag.Order = order;

            var _usZipRegEx = @"^\d{5}(?:[-\s]\d{4})?$";
            var _caZipRegEx = @"^([ABCEGHJKLMNPRSTVXY]\d[ABCEGHJKLMNPRSTVWXYZ])\ {0,1}(\d[ABCEGHJKLMNPRSTVWXYZ]\d)$";


            if (model.Country == "ca")
            {




                if (model.Province == null)
                {
                    ModelState.AddModelError("Province", "*");
                }
                if (model.PostalCode == null)
                {
                    ModelState.AddModelError("PostalCode", "*");
                }
                else
                {
                    if (!Regex.Match(model.PostalCode, _caZipRegEx).Success)
                    {
                        ModelState.AddModelError("PostalCode", "Invalid Postal Code");
                    }
                }
            }
            else if (model.Country == "us")
            {
                if (model.State == null)
                {
                    ModelState.AddModelError("State", "*");
                }
                if (model.Zip == null)
                {
                    ModelState.AddModelError("Zip", "*");
                }
                else
                {
                    if (!Regex.Match(model.Zip, _usZipRegEx).Success)
                    {
                        ModelState.AddModelError("Zip", "Invalid Zip");
                    }
                }
            }

            if (ModelState.IsValid)
            {

                var nonfoundation = 0;
                var priceExists = 0M;

                try
                {

                    foreach (var productInOrder in order.DTMProductsInOrder)
                    {

                        if (productInOrder.Product.Availability == false)
                        {

                            productInOrder.Status = "OnHold";
                            order.Status = "OnHold";

                        }
                        //else
                        //{
                        //    productInOrder.Status = "Approved";
                        //}

                        nonfoundation++;

                        var finalpoints = GetFinalPoints(productInOrder.Product.Id, productInOrder.PurchaseOption);
                        productInOrder.Points = finalpoints;

                        var finalprice = GetFinalPrice(productInOrder.Product.Id, productInOrder.PurchaseOption);
                        productInOrder.Price = finalprice;


                        if (finalprice != 0.0M)
                        {
                            priceExists = finalprice;
                        }
                        //else 
                        //{
                        //    productInOrder.Status = "Approved";
                        //}

                    }
                }
                catch
                {

                }
                order.DateSubmitted = DateTime.UtcNow;
                order.Recipient = model.Recipient;
                order.Address = model.Address;
                order.Address2 = model.Address2;
                order.City = model.City;
                order.PhoneNumber = model.Phone;

                order.Zip = model.Zip;
                order.State = model.State;
                order.Country = "United States";

                if (order.Status == "OnHold")
                {
                    db.SaveChanges();
                    return RedirectToAction("MDTUnavailableOrderConfirm", new { Id = order.Id });
                }
                else if (priceExists != 0.0M)
                {
                    order.Status = "Pending";
                    order.DTMProductsInOrder.Where(x => x.DTMOrder.Id == order.Id).FirstOrDefault().Status = "Pending";
                    db.SaveChanges();
                    return RedirectToAction("PaymentDetails", new { Id = order.Id });
                }
                else
                {
                    order.Status = "Approved";
                    order.DTMProductsInOrder.Where(x => x.DTMOrder.Id == order.Id).FirstOrDefault().Status = "Approved";
                    db.SaveChanges();
                    return RedirectToAction("MDTOrderConfirm", new { Id = order.Id });
                }             
              
            }
            else
            {
                var modelErrors = new List<string>();
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var modelError in modelState.Errors)
                    {
                        modelErrors.Add(modelError.ErrorMessage);
                    }
                }

                var result = String.Join(", ", modelErrors.ToArray());

                //var products = from p in db.DTMProductinOrder.Where(x => x.DTMOrder.Id == order.Id)
                //               select new
                //               {
                //                   ProductName = p.Product.Model,
                //                   Quantity = p.Quantity,
                //                   Points = p.Product.Catalog.RewardAmount * p.Quantity,
                //                   UofM = p.Product.Catalog.UofM
                //               };
                //var productlist = products.ToList().OrderBy(x => x.ProductName);

                var products = from p in db.DTMProductinOrder.Where(x => x.DTMOrder.Id == order.Id)
                               select new
                               {
                                   Id = p.Product.Id,
                                   ProductName = p.Product.Model,
                                   Quantity = p.Quantity,
                                   MSRP = p.PurchaseOption,
                                   Available = p.Product.Availability,
                                   Brand = p.Product.Brand,
                                   Category = p.Product.Category


                               };
                List<DTMCheckout> productlist = new List<DTMCheckout>();

                foreach (var item in products)
                {
                    var finalprice = GetFinalPrice(item.Id, item.MSRP);
                    var finalpoints = GetFinalPoints(item.Id, item.MSRP);

                    var list = new DTMCheckout()
                    {
                        Id = item.Id,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        MSRP = item.MSRP,
                        Available = item.Available == true ? "Available" : "Unavailable",
                        Price = finalprice * item.Quantity,
                        Points = finalpoints * item.Quantity,
                        Brand = item.Brand,
                        Category = item.Category
                    };

                    productlist.Add(list);
                }
                var orderpricetotal = order.CalculatePrice();

                ViewBag.WebGrid = new WebGrid(productlist, rowsPerPage: 10);
                var ordertotal = order.CalculatePoints();
                ViewBag.OrderTotal = ordertotal;
                ViewBag.OrderTotalPrice = orderpricetotal;


                if (products.Count() == 0)
                {
                    return Redirect("Index");
                }
                model.Country = "us";
                return View(model);
            }
        }


        public ActionResult MDTOrderConfirm(Guid Id)
        {

            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var orders = db.DTMOrder.Where(x => x.Participant.Id == participant.Id);

            var redemption = orders.Where(x => x.Id == Id).First();

            if (redemption.Status == "Pending") 
            {
                var productsinorder = db.DTMProductinOrder.Where(x => x.DTMOrder.Id == redemption.Id).ToList();

                foreach (var item in productsinorder) 
                {
                    if (item.Status != "Cancelled")
                    {
                        item.Status = "Approved";
                    }
                    db.SaveChanges();
                }
                redemption.Status = "Approved";
                db.SaveChanges();
            
            }


            var products = from p in db.DTMProductinOrder.Where(x => x.DTMOrder.Id == redemption.Id && x.Status != "Cancelled")
                           select new
                           {
                               Id = p.Product.Id,
                               ProductName = p.Product.Model,
                               Quantity = p.Quantity,
                               MSRP = p.PurchaseOption,
                               Available = p.Product.Availability,
                               Brand = p.Product.Brand,
                               Category = p.Product.Category
                           };

            var totalpoints = redemption.CalculatePoints().ToString("F0");

            var items = "<table><tr><th>Quantity</th><th>Item</th><th>Purchase Option</th><th>Points</th><th>Price</th></tr>";

            foreach (var product in products)
            {
                var pointtotal = GetFinalPoints(product.Id, product.MSRP).ToString("F0");
                var pricetotal = GetFinalPrice(product.Id, product.MSRP).ToString("F2");

                items = items + "<tr><td>" + product.Quantity.ToString("F0") + "</td><td>" + product.Brand + " - " + product.Category + " - " + product.ProductName + "</td><td>" + product.MSRP + "</td><td>" + pointtotal + "</td><td>$" + pricetotal + "</td></tr>";
            }

            items = items + "</table>";

            var address = "";

            address = redemption.Recipient + "<br/>" + redemption.PhoneNumber + "<br/>" + redemption.Address + "<br/>";

            if (redemption.Address2 != null)
            {
                address = address + redemption.Address2 + "<br/>";
            }

            address = address + redemption.City + ", " + redemption.State + "  " + redemption.Zip;


            RedemptionViewModel model = new RedemptionViewModel();

            model.Address = redemption.Address;
            model.Address2 = redemption.Address2;
            model.City = redemption.City;
            model.Recipient = redemption.Recipient;
            model.Phone = redemption.PhoneNumber;
            model.State = redemption.State;
            model.Zip = redemption.Zip;
            model.Country = redemption.Country;
            model.SequentialId = redemption.SequentialId ?? 0;
            //model.SequentialId = redemption.SequentialId ?? "0";
            model.TotalPoints = redemption.CalculatePoints();
            model.TotalPrice = redemption.CalculatePrice();

            var tax = 0M;         
            var total = 0M;

            if (model.TotalPrice != 0) 
            {
                var data = db.PointTransaction.Where(x => x.Description.Substring(14, x.Description.Length) == redemption.SequentialId.ToString()).FirstOrDefault();
                ViewBag.Tax = data;
                tax = data.Tax??0;
                total = data.Total??0;
                
            }

            List<DTMCheckout> productlist = new List<DTMCheckout>();

            foreach (var item in products)
            {
                var finalprice = GetFinalPrice(item.Id, item.MSRP);
                var finalpoints = GetFinalPoints(item.Id, item.MSRP);

                var list = new DTMCheckout()
                {
                    Id = item.Id,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    MSRP = item.MSRP,
                    Available = item.Available == true ? "Available" : "Unavailable",
                    Price = finalprice * item.Quantity,
                    Points = finalpoints * item.Quantity,
                    Brand = item.Brand,
                    Category = item.Category
                };

                productlist.Add(list);
            }




            //  var productlist = products.ToList().OrderBy(x => x.ProductName);

            ViewBag.Order = redemption;
            ViewBag.WebGrid = new WebGrid(productlist, rowsPerPage: 10);
        

            var emailbody = participant.FirstName + ",<br/><br/>Your order is being processed!";
            emailbody = emailbody + "<br/><br/>Here are the details:<br/><br/>Order Number: " + redemption.SequentialId + "<br/><br/> Item(s):<br/> " + items + "<br/><br/> Total Points Redeemed: " + totalpoints + "<br/>Sub Total: $" + model.TotalPrice.ToString("F2") + "<br/>Tax: $" + tax.ToString("F2") + "<br/>Total: $" + total.ToString("F2")  + "<br/><br/> Delivery Address: <br/>" + address;
            emailbody = emailbody + "<br/><br/>Are you excited as we are and want to track your order? You can do that anytime by going to <a href='soundunited.karrotrewards.com'>soundunited.karrotrewards.com</a>. You’ll also receive an email notification when your order has been processed.";
            emailbody = emailbody + "<br/><br/>We look forward to sending you more rewards! <br/><br/>Happy Selling,<br/><br/>Sound United Rewards Program Headquarters";

            if (redemption.EmailSent != 1)
            {
                SmtpMailer.Send(
                    subject: "Your Marantz & Definitive Technology Order has been placed!",
                    body: emailbody,
                    to: participant.Email,
                    bcc: ""
                    );

                try
                {

                }
                catch (Exception e)
                {

                }

                redemption.EmailSent = 1;
                db.SaveChanges();
            }
            return View(model);
        }



        public ActionResult MDTUnavailableOrderConfirm(Guid Id)
        {

            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var orders = db.DTMOrder.Where(x => x.Participant.Id == participant.Id);

            var redemption = orders.Where(x => x.Id == Id).First();


            var products = from p in db.DTMProductinOrder.Where(x => x.DTMOrder.Id == redemption.Id)
                           select new
                           {
                               Id = p.Product.Id,
                               ProductName = p.Product.Model,
                               Quantity = p.Quantity,
                               MSRP = p.PurchaseOption,
                               Available = p.Product.Availability,
                               Brand = p.Product.Brand,
                               Category = p.Product.Category
                           };

            var totalpoints = redemption.CalculatePoints().ToString("F0");

            var items = "<table><tr><th>Quantity</th><th>Item</th><th>Purchase Option</th><th>Points</th><th>Price</th></tr>";

            foreach (var product in products)
            {
                var pointtotal = GetFinalPoints(product.Id, product.MSRP).ToString("F0");
                var pricetotal = GetFinalPrice(product.Id, product.MSRP).ToString("F0");

                items = items + "<tr><td>" + product.Quantity.ToString("F0") + "</td><td>" + product.Brand + " - " + product.Category + " - " + product.ProductName + "</td><td>" + product.MSRP + "</td><td>" + pointtotal + "</td><td>" + pricetotal + "</td></tr>";
            }

            items = items + "</table>";

            var address = "";

            address = redemption.Recipient + "<br/>" + redemption.PhoneNumber + "<br/>" + redemption.Address + "<br/>";

            if (redemption.Address2 != null)
            {
                address = address + redemption.Address2 + "<br/>";
            }

            address = address + redemption.City + ", " + redemption.State + "  " + redemption.Zip;


            RedemptionViewModel model = new RedemptionViewModel();

            model.Address = redemption.Address;
            model.Address2 = redemption.Address2;
            model.City = redemption.City;
            model.Recipient = redemption.Recipient;
            model.Phone = redemption.PhoneNumber;
            model.State = redemption.State;
            model.Zip = redemption.Zip;
            model.Country = redemption.Country;
            model.SequentialId = redemption.SequentialId ?? 0;
            //model.SequentialId = redemption.SequentialId ?? "0";
            model.TotalPoints = redemption.CalculatePoints();
            model.TotalPrice = redemption.CalculatePrice();



            List<DTMCheckout> productlist = new List<DTMCheckout>();

            foreach (var item in products)
            {
                var finalprice = GetFinalPrice(item.Id, item.MSRP);
                var finalpoints = GetFinalPoints(item.Id, item.MSRP);

                var list = new DTMCheckout()
                {
                    Id = item.Id,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    MSRP = item.MSRP,
                    Available = item.Available == true ? "Available" : "Unavailable",
                    Price = finalprice * item.Quantity,
                    Points = finalpoints * item.Quantity,
                    Brand = item.Brand,
                    Category = item.Category
                };

                productlist.Add(list);
            }




            //  var productlist = products.ToList().OrderBy(x => x.ProductName);

            ViewBag.Order = redemption;
            ViewBag.WebGrid = new WebGrid(productlist, rowsPerPage: 10);

            //var emailbody = participant.FirstName + ",<br/><br/>Your order is being processed!";
            //emailbody = emailbody + "<br/><br/>Here are the details:<br/><br/>Order Number: " + redemption.SequentialId + "<br/><br/> Item(s):<br/> " + items + "<br/><br/> Total Points Redeemed: " + totalpoints + "<br/><br/> Delivery Address: <br/>" + address;
            //emailbody = emailbody + "<br/><br/>Are you excited as we are and want to track your order? You can do that anytime by going to <a href='soundunited.karrotrewards.com'>soundunited.karrotrewards.com</a>. You’ll also receive an email notification when your order has been processed.";
            //emailbody = emailbody + "<br/><br/>We look forward to sending you more rewards! <br/><br/>Happy Selling,<br/><br/>Sound United Rewards Program Headquarters";

            //if (redemption.EmailSent != 1)
            //{
            //    SmtpMailer.Send(
            //        subject: "Your Marantz & Definitive Technology Order has been placed!",
            //        body: emailbody,
            //        to: participant.Email,
            //        bcc: ""
            //        );

            //    try
            //    {

            //    }
            //    catch (Exception e)
            //    {

            //    }

            //    redemption.EmailSent = 1;
            //    db.SaveChanges();
            //}
            return View(model);
        }


        public ActionResult UpdateOrder(string orderid)
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            //var request = Request.QueryString["id"];
            var id = new Guid(orderid);

            Bowers.Models.DTMOrder order;
            order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Id == id);


            ViewBag.Order = order;

            var pid = new Guid(User.Identity.GetUserId());

            var pointtotal = participant.CalculatePoints(participant.Id);

            ViewBag.PointTotal = pointtotal;

            var ordertotal = order.CalculatePoints();
            var orderpricetotal = order.CalculatePrice();

            CheckoutModel model = new CheckoutModel();
            model.Country = "us";
            model.Recipient = order.Recipient;
            model.Address = order.Address;
            model.City = order.City;
            model.State = order.State;
            model.Zip = order.Zip;
            model.Phone = order.PhoneNumber;
            model.Id = order.Id;

            var products = from p in db.DTMProductinOrder.Where(x => x.DTMOrder.Id == order.Id)
                           select new
                           {
                               Id = p.Product.Id,
                               ProductName = p.Product.Model,
                               Quantity = p.Quantity,
                               MSRP = p.PurchaseOption,
                               Available = p.Product.Availability,
                               Brand = p.Product.Brand,
                               Category = p.Product.Category,


                           };
            List<DTMCheckout> productlist = new List<DTMCheckout>();

            foreach (var item in products)
            {
                var finalprice = GetFinalPrice(item.Id, item.MSRP);
                var finalpoints = GetFinalPoints(item.Id, item.MSRP);

                var list = new DTMCheckout()
                {
                    Id = item.Id,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    MSRP = item.MSRP,
                    Available = item.Available == true ? "Available" : "Unavailable",
                    Price = finalprice * item.Quantity,
                    Points = finalpoints * item.Quantity,
                    Brand = item.Brand,
                    Category = item.Category
                };

                productlist.Add(list);
            }

            ViewBag.WebGrid = new WebGrid(productlist, rowsPerPage: 10);
            ViewBag.OrderTotal = ordertotal;
            ViewBag.OrderTotalPrice = orderpricetotal;


            if (order.Status == "Pending") 
            {
                ViewBag.FinishOrder = "true";
            }

            return View(model);

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrder(CheckoutModel model, string orderid)
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            //var request = Request.QueryString["id"];
            var id = new Guid(orderid);

            Bowers.Models.DTMOrder order;
            order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Id == model.Id);


            ViewBag.Order = order;
            var pid = new Guid(User.Identity.GetUserId());

            var pointtotal = participant.CalculatePoints(participant.Id);

            ViewBag.PointTotal = pointtotal;

            var ordertotal = order.CalculatePoints();
            var orderpricetotal = order.CalculatePrice();



            var products = from p in db.DTMProductinOrder.Where(x => x.DTMOrder.Id == order.Id)
                           select new
                           {
                               Id = p.Product.Id,
                               ProductName = p.Product.Model,
                               Quantity = p.Quantity,
                               MSRP = p.PurchaseOption,
                               Available = p.Product.Availability,
                               Brand = p.Product.Brand,
                               Category = p.Product.Category,


                           };
            List<DTMCheckout> productlist = new List<DTMCheckout>();

            foreach (var item in products)
            {
                var finalprice = GetFinalPrice(item.Id, item.MSRP);
                var finalpoints = GetFinalPoints(item.Id, item.MSRP);

                var list = new DTMCheckout()
                {
                    Id = item.Id,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    MSRP = item.MSRP,
                    Available = item.Available == true ? "Available" : "Unavailable",
                    Price = finalprice * item.Quantity,
                    Points = finalpoints * item.Quantity,
                    Brand = item.Brand,
                    Category = item.Category
                };

                productlist.Add(list);
            }

            ViewBag.WebGrid = new WebGrid(productlist, rowsPerPage: 10);
            ViewBag.OrderTotal = ordertotal;
            ViewBag.OrderTotalPrice = orderpricetotal;

            if (ModelState.IsValid)
            {

                //  CheckoutModel mdel = new CheckoutModel();
                //  order.Country = "us";
                order.Recipient = model.Recipient;
                order.Address = model.Address;
                order.City = model.City;
                order.State = model.State;
                order.Zip = model.Zip;
                order.PhoneNumber = model.Phone;
                db.SaveChanges();
                ModelState.AddModelError("", "Order Updated");

            }
            else 
            {
                ModelState.AddModelError("", "All fields are required");
            }




            model.Id = model.Id;
            return View(model);

        }

        public ActionResult DeleteOrder(string orderid) 
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            //var request = Request.QueryString["id"];
            var id = new Guid(orderid);

            Bowers.Models.DTMOrder order;
            order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Id == id);

           
                foreach (var item in order.DTMProductsInOrder)
                {
                    db.DTMProductinOrder.Remove(item);
                    db.SaveChanges();

                    if (order.DTMProductsInOrder.Count <= 0) 
                    {
                    break;
                    }
                }        

            db.DTMOrder.Remove(order);
            db.SaveChanges();     


            return RedirectToAction("DTMCatalog");
        }

        public ActionResult Checkout()
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            try
            {
                Bowers.Models.Order order;
                order = db.Order.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");

                try
                {
                    foreach (var productInOrder in order.ProductsInOrder)
                    {
                        if (productInOrder.Product.Status != "Available")
                        {
                            order.ProductsInOrder.Remove(productInOrder);
                            var fredemption = db.ProductinOrder.Where(x => x.Status != "Cancelled");

                        }

                    }
                }
                catch
                {
                }

                db.SaveChanges();

                ViewBag.Order = order;

                var pid = new Guid(User.Identity.GetUserId());

                var pointtotal = participant.CalculatePoints(participant.Id);

                ViewBag.PointTotal = pointtotal;

                var ordertotal = order.CalculatePoints();


                if (pointtotal < ordertotal || (pointtotal - ordertotal) < 0 || pointtotal < 0)
                {
                    return RedirectToAction("Cart", new { over = true });
                }


                var products = from p in db.ProductinOrder.Where(x => x.Order.Id == order.Id)
                               select new
                               {
                                   ProductName = p.Product.Catalog.Model,
                                   Quantity = p.Quantity,
                                   Points = p.Product.Catalog.RewardAmount * p.Quantity,
                                   UofM = p.Product.Catalog.UofM
            };
                var productlist = products.ToList().OrderBy(x => x.ProductName);

                ViewBag.WebGrid = new WebGrid(productlist, rowsPerPage: 10);
                ViewBag.OrderTotal = ordertotal;

                if (products.Count() == 0)
                {
                    return Redirect("Index");
                }

            }
            catch (Exception e)
            {
                return Redirect("Index");
            }

            CheckoutModel model = new CheckoutModel();
            model.Country = "us";
            return View(model);

        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutModel model)
        {
            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var pointtotal = participant.CalculatePoints(participant.Id);
            var order = db.Order.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");
            var ordertotal = order.CalculatePoints();
            ViewBag.Order = order;

            var _usZipRegEx = @"^\d{5}(?:[-\s]\d{4})?$";
            var _caZipRegEx = @"^([ABCEGHJKLMNPRSTVXY]\d[ABCEGHJKLMNPRSTVWXYZ])\ {0,1}(\d[ABCEGHJKLMNPRSTVWXYZ]\d)$";


            if (model.Country == "ca")
            {
                if(model.Province == null)
                {
                    ModelState.AddModelError("Province", "*");
                }
                if (model.PostalCode == null)
                {
                    ModelState.AddModelError("PostalCode", "*");
                } else
                {
                    if (!Regex.Match(model.PostalCode, _caZipRegEx).Success)
                    {
                        ModelState.AddModelError("PostalCode", "Invalid Postal Code");
                    }
                }
            }
            else if (model.Country == "us")
            {
                if (model.State == null)
                {
                    ModelState.AddModelError("State", "*");
                }
                if (model.Zip == null)
                {
                    ModelState.AddModelError("Zip", "*");
                } else
                {
                    if (!Regex.Match(model.Zip, _usZipRegEx).Success)
                    {
                        ModelState.AddModelError("Zip", "Invalid Zip");
                    }
                }
            }

            if (ModelState.IsValid)
            {

                var nonfoundation = 0;

                try
                {

                    foreach (var productInOrder in order.ProductsInOrder)
                    {
                        nonfoundation++;
                        productInOrder.Status = "Approved";

                        productInOrder.Points = (decimal)productInOrder.Product.Catalog.RewardAmount;

                    }
                }
                catch
                {

                }

                if (pointtotal < ordertotal || (pointtotal - ordertotal) < 0 || pointtotal < 0)
                {
                    return RedirectToAction("Cart", new { over = true });
                }
                else
                {
                    order.DateSubmitted = DateTime.UtcNow;
                    order.Recipient = model.Recipient;
                    order.Address = model.Address;
                    order.Address2 = model.Address2;
                    order.City = model.City;
                    order.Status = "Approved";

                    if (!String.IsNullOrEmpty(model.Phone))
                    {
                        order.PhoneNumber = model.Phone;
                    }
                    else
                    {
                        order.PhoneNumber = "";
                    }


                    if (model.Country == "ca")
                    {
                        order.Zip = model.PostalCode;
                        order.State = model.Province;
                        order.Country = "Canada";
                    }
                    else
                    {
                        order.Zip = model.Zip;
                        order.State = model.State;
                        order.Country = "United States";
                    }
                    db.SaveChanges();
                    return RedirectToAction("OrderConfirm", new { Id = order.Id });
                }
            }
            else
            {
                var modelErrors = new List<string>();
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var modelError in modelState.Errors)
                    {
                        modelErrors.Add(modelError.ErrorMessage);
                    }
                }

                var result = String.Join(", ", modelErrors.ToArray());

                var products = from p in db.ProductinOrder.Where(x => x.Order.Id == order.Id)
                               select new
                               {
                                   ProductName = p.Product.Catalog.Model,
                                   Quantity = p.Quantity,
                                   Points = p.Product.Catalog.RewardAmount * p.Quantity,
                                   UofM = p.Product.Catalog.UofM
                               };
                var productlist = products.ToList().OrderBy(x => x.ProductName);

                ViewBag.WebGrid = new WebGrid(productlist, rowsPerPage: 10);
                ViewBag.OrderTotal = ordertotal;

                if (products.Count() == 0)
                {
                    return Redirect("Index");
                }
                model.Country = "us";
                return View(model);
           
            } 
        
        
        
        }
        
    
    

        public ActionResult OrderConfirm(Guid Id)
        {

            var db = new ApplicationDbContext();
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            var orders = db.Order.Where(x => x.Participant.Id == participant.Id);

            var redemption = orders.Where(x => x.Id == Id).First();


            var products = from p in db.ProductinOrder.Where(x => x.Order.Id == redemption.Id)
                           select new
                           {
                               ProductName = p.Product.Catalog.Model,
                               Size = p.Product.Finish,
                               Quantity = p.Quantity,
                               Points = p.Points * p.Quantity,
                               UofM = p.Product.Catalog.UofM
                           };

            var totalpoints = Convert.ToInt32(products.Sum(x => x.Points)).ToString("F0");

            var items = "<table><tr><th>Quantity</th><th>Item</th><th>Points</th></tr>";

            foreach (var product in products)
            {
                var pointtotal = Convert.ToInt32(product.Points).ToString("F0");

                items = items + "<tr><td>" + product.Quantity.ToString("F0") + "</td><td>" + product.ProductName + " - " + product.Size + "</td><td>" + pointtotal + "</td></tr>";
            }

            items = items + "</table>";

            var address = "";

            address = redemption.Recipient + "<br/>" + redemption.PhoneNumber + "<br/>" + redemption.Address + "<br/>";

            if (redemption.Address2 != null)
            {
                address = address + redemption.Address2 + "<br/>";
            }

            address = address + redemption.City + ", " + redemption.State + "  " + redemption.Zip;


            RedemptionViewModel model = new RedemptionViewModel();

            model.Address = redemption.Address;
            model.Address2 = redemption.Address2;
            model.City = redemption.City;
            model.Recipient = redemption.Recipient;
            model.Phone = redemption.PhoneNumber;
            model.State = redemption.State;
            model.Zip = redemption.Zip;
            model.Country = redemption.Country;
            model.SequentialId = redemption.SequentialId ?? 0;
            //model.SequentialId = redemption.SequentialId ?? "0";
            model.TotalPoints = products.Sum(x => x.Points);

            var productlist = products.ToList().OrderBy(x => x.ProductName);

            ViewBag.Order = redemption;
            ViewBag.WebGrid = new WebGrid(productlist, rowsPerPage: 10);

            var emailbody = participant.FirstName + ",<br/><br/>Your order is being processed!";
            emailbody = emailbody + "<br/><br/>Here are the details:<br/><br/>Order Number: " + redemption.SequentialId + "<br/><br/> Item(s):<br/> " + items + "<br/><br/> Total Points Redeemed: " + totalpoints + "<br/><br/> Delivery Address: <br/>" + address;
            emailbody = emailbody + "<br/><br/>Are you excited as we are and want to track your order? You can do that anytime by going to <a href='soundunited.karrotrewards.com'>soundunited.karrotrewards.com</a>. You’ll also receive an email notification when your order has been processed.";
            emailbody = emailbody + "<br/><br/>We look forward to sending you more rewards! <br/><br/>Happy Selling,<br/><br/>Sound United Rewards Program Headquarters";

            if (redemption.EmailSent != 1)
            {
                SmtpMailer.Send(
                    subject: "You have a Bowers & Wilkins reward order being processed.",
                    body: emailbody,
                    to: participant.Email,
                    bcc: ""
                    );

                try
                {
                   
                }
                catch (Exception e)
                {

                }

                redemption.EmailSent = 1;
                db.SaveChanges();
            }
            return View(model);
        }
        public async Task<ActionResult> PaymentDetails(string Id) 
        {

            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));

            var id = Guid.Parse(Id);


            await APIPullAsyncMDT(id);


            Bowers.Models.DTMOrder order;
            order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Id == id);

            if (order.Status == "Pending" && order.CalculatePrice() == 0M) 
            {
                order.Status = "Approved";
                return RedirectToAction("MDTOrderConfirm", new { Id = order.Id });
            
            }



            ViewBag.Order = order;
            order.Status = "Pending";
            db.SaveChanges();

            var pid = new Guid(User.Identity.GetUserId());
        
            ViewBag.Dollars = order.CalculatePrice();

          

            return View();
        }



        public ActionResult PurchasePoints()
        {
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));

            Bowers.Models.Order order;
            order = db.Order.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");
            ViewBag.Order = order;

            var pid = new Guid(User.Identity.GetUserId());

            var pointtotal = participant.CalculatePoints(participant.Id);

            ViewBag.PointTotal = pointtotal;

            var ordertotal = order.CalculatePoints();

            var pointsneeded = ordertotal - pointtotal;

            ViewBag.Purchase = pointsneeded;
            ViewBag.Dollars = pointsneeded * .025M;

            APIPullAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SubmitDTM(string orderid)
        {
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            Bowers.Models.DTMOrder order;

            var ID = Guid.Parse(orderid);           
            order = db.DTMOrder.First(e => e.Participant.Id == participant.Id && e.Id == ID);
            ViewBag.Order = order;        

            var pid = new Guid(User.Identity.GetUserId());

            var pointtotal = participant.CalculatePoints(participant.Id);

            ViewBag.PointTotal = pointtotal;

            //var ordertotal = order.CalculatePoints();

            var pricesneeded = order.CalculatePrice();

            ViewBag.Purchase = pricesneeded;

            var amount = pricesneeded;
            ViewBag.Dollars = amount;

            taxTotal = Decimal.Round(Convert.ToDecimal(await APIPullTaxAsyncMDT(ID)), 2);

            try
            {
                StripeConfiguration.ApiKey = Global.StripeSecretKey;
                var stripeTokenString = Request.Form["stripeToken"];
                var myCharge = new ChargeCreateOptions();

                myCharge.Amount = (int)(((amount + taxTotal) * 100));
                myCharge.Currency = "usd";
                myCharge.Description = "DT&M product Purchase: " + participant.Email + " / " + participant.Id + " / OrderId: " + order.SequentialId;

                myCharge.Source = stripeTokenString;

                var chargeService = new ChargeService();
                Charge stripeCharge = chargeService.Create(myCharge);

                if (stripeCharge.Status == "succeeded")
                {
                    var pointtransaction = new PointTransaction(db)
                    {
                        Participant = participant,
                        RewardAmount = 0M,
                        PurchaseAmount = amount,
                        Category = "Product Purchase",
                        Description = "Order Number: " + order.SequentialId,
                        Status = stripeCharge.Status,
                        Tax = taxTotal,
                        Total = amount + taxTotal
                    };

                    foreach (var item in order.DTMProductsInOrder)
                    {
                        if (item.Status != "Cancelled") 
                        {
                            item.Status = "Approved";
                        }                        
                        db.SaveChanges();
                    }
                    order.Status = "Approved";
                    db.SaveChanges();
                    db.PointTransaction.Add(pointtransaction);
                    db.SaveChanges();
                    return RedirectToAction("MDTOrderConfirm", new { Id = order.Id });
                }
                else
                {
                    ViewBag.Feedback = "Purchase failed: Payment could not be processed.";
                    return View("PaymentDetails");
                }
            }
            catch (Exception e)
            {
                ViewBag.Feedback = "Purchase failed: " + e.Message;
                return View("PaymentDetails");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Submit()
        {
            var participant = db.Participant.Find(new Guid(User.Identity.GetUserId()));
            Bowers.Models.Order order;
            order = db.Order.First(e => e.Participant.Id == participant.Id && e.Status == "Unsubmitted");
            ViewBag.Order = order;

            var pid = new Guid(User.Identity.GetUserId());

            var pointtotal = participant.CalculatePoints(participant.Id);

            ViewBag.PointTotal = pointtotal;

            var ordertotal = order.CalculatePoints();

            var pointsneeded = ordertotal - pointtotal;

            ViewBag.Purchase = pointsneeded;

            var amount = pointsneeded * .025M;
            ViewBag.Dollars = amount;

            taxTotal = Decimal.Round(Convert.ToDecimal(await APIPullTaxAsync()),2);

            try
            {
                StripeConfiguration.ApiKey = Global.StripeSecretKey;
                var stripeTokenString = Request.Form["stripeToken"];
                var myCharge = new ChargeCreateOptions();

                myCharge.Amount = (int)(((amount + taxTotal) * 100));
                myCharge.Currency = "usd";
                myCharge.Description = "B&W Point Purchase: " + participant.Email + " / " + participant.Id + " / OrderId: " + order.SequentialId;

                myCharge.Source = stripeTokenString;

                var chargeService = new ChargeService();
                Charge stripeCharge = chargeService.Create(myCharge);

                if (stripeCharge.Status == "succeeded")
                {
                    var pointtransaction = new PointTransaction(db)
                    {
                        Participant = participant,
                        RewardAmount = pointsneeded,
                        PurchaseAmount = amount,
                        Category = "Point Purchase",
                        Description = "Order Number: " + order.SequentialId,
                        Status = stripeCharge.Status,
                        Tax = taxTotal,
                        Total = amount + taxTotal
                    };

                    db.PointTransaction.Add(pointtransaction);
                    db.SaveChanges();
                    return Redirect("~/Participant/Page/Checkout");
                }
                else
                {
                    ViewBag.Feedback = "Purchase failed: Payment could not be processed.";
                    return View("PurchasePoints");
                }
            }
            catch (Exception e)
            {
                ViewBag.Feedback = "Purchase failed: " + e.Message;
                return View("PurchasePoints");
            }
        }

        public class SupplierEvidence
        {
            public string zip { get; set; }
            public string state { get; set; }
            public string country { get; set; }
            public string zone { get; set; }
        }

        public class SupplierLocalization
        {
            public string zip { get; set; }
            public string state { get; set; }
            public string country { get; set; }
            public string zone { get; set; }
        }

        public class Billing
        {
            public string zip { get; set; }
            public string state { get; set; }
            public string country { get; set; }
        }

        public class CustomerEvidence
        {
            public object tax_id { get; set; }
            public Billing billing { get; set; }
            public object ip { get; set; }
            public object payment_source { get; set; }
        }

        public class CustomerLocalization
        {
            public string zip { get; set; }
            public string state { get; set; }
            public string country { get; set; }
            public string zone { get; set; }
        }

        public class TaxDetail
        {
            public string name { get; set; }
            public double rate { get; set; }
            public string type { get; set; }
            public bool reduced { get; set; }
            public string tax { get; set; }
            public string tax_name { get; set; }
        }

        public class APIData
        {
            public string id { get; set; }
            public string @object { get; set; }
            public bool livemode { get; set; }
            public string sale_mode { get; set; }
            public string product_type { get; set; }
            public SupplierEvidence supplier_evidence { get; set; }
            public SupplierLocalization supplier_localization { get; set; }
            public CustomerEvidence customer_evidence { get; set; }
            public CustomerLocalization customer_localization { get; set; }
            public object supplier_tax_id { get; set; }
            public object warehouse { get; set; }
            public bool tax_enabled { get; set; }
            public string tax { get; set; }
            public string tax_code { get; set; }
            public string tax_zone { get; set; }
            public string declare_in_region { get; set; }
            public string customer_region { get; set; }
            public double applied_rate { get; set; }
            public List<TaxDetail> tax_details { get; set; }
            public object tax_id_validation { get; set; }
        }
    }
}