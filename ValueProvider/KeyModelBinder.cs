using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ValueProvider
{
    /// <summary>
    /// https://tahirnaushad.com/2017/09/15/custom-model-binding-in-asp-net-core-2-0/
    /// https://github.com/aspnet/Mvc/issues/4553
    /// </summary>
    public class AlternativeKeyModelBinder : IModelBinder
    {
        
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var _alternatives_ = new List<string>(bindingContext.FieldName.Split(",", StringSplitOptions.RemoveEmptyEntries));
            _alternatives_.Insert(0, bindingContext.ModelMetadata.PropertyName);

            var valueProviderResult = ValueProviderResult.None;
            foreach (var a in _alternatives_)
            {
                valueProviderResult = bindingContext.ValueProvider.GetValue(a);
                if (valueProviderResult != ValueProviderResult.None)
                {
                    bindingContext.ModelState.SetModelValue(a, valueProviderResult);

                    bindingContext.Result = ModelBindingResult.Success(valueProviderResult.FirstValue);
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }
    }


    public class BIND
    {
        //[ModelBinder(BinderType = typeof(AlternativeKeyModelBinder), Name = "XML")]
        public string query { get; set; }
        public string formencoded { get; set; }
        public string formdata { get; set; }
        public string route { get; set; }
    }
}
