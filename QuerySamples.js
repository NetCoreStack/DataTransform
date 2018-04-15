// to list all the collections and document count for a specific mongodb database
var collections = db.getCollectionNames();

print('Collections inside the db:');
for(var i = 0; i < collections.length; i++){
  var name = collections[i];

  if(name.substr(0, 6) != 'system')
    print(name + ' - ' + db[name].count() + ' records');
}